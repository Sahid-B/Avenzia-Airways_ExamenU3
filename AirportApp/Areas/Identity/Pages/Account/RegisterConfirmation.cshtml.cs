// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace AirportApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _sender;

        public RegisterConfirmationModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IEmailSender sender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _sender = sender;
        }

        public string Email { get; set; }
        public string ReturnUrl { get; set; }
        public bool DisplayConfirmAccountLink { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "El código de verificación es obligatorio.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener exactamente 6 dígitos.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe contener solo números.")]
        public string VerificationCode { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }
            ReturnUrl = returnUrl ?? Url.Content("~/");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            Email = email;
            DisplayConfirmAccountLink = false; // Disable fake auto-confirmation link
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string email, string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            Email = email;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var codeClaim = claims.FirstOrDefault(c => c.Type == "EmailConfirmationCode");

            if (codeClaim != null && codeClaim.Value == VerificationCode.Trim())
            {
                // Confirm the email address
                user.EmailConfirmed = true;
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    // Clean up the claim
                    await _userManager.RemoveClaimAsync(user, codeClaim);

                    // Sign the user in
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(ReturnUrl);
                }

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "El código de verificación es incorrecto o ha expirado.");
            }

            return Page();
        }
    }
}
