// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
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
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ResendEmailConfirmationModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal user existence
                return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
            }

            // Generate new 6-digit code
            string code = new Random().Next(100000, 999999).ToString("D6");

            // Replace existing confirmation code claim
            var claims = await _userManager.GetClaimsAsync(user);
            var oldClaim = claims.FirstOrDefault(c => c.Type == "EmailConfirmationCode");
            if (oldClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, oldClaim);
            }
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("EmailConfirmationCode", code));

            // Send email
            await _emailSender.SendEmailAsync(Input.Email, "Código de Verificación - Avenzia Airways",
                $"Hola,<br/><br/>Tu nuevo código de activación para activar tu cuenta es:<br/><br/>" +
                $"<h2 style='color:#7E91B3; font-weight:bold; letter-spacing:2px;'>{code}</h2><br/>" +
                $"Ingresa este código en la pantalla de verificación para continuar.");

            return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
        }
    }
}
