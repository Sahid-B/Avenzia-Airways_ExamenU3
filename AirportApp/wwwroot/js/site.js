// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", function() {
    // Style any plain CRUD action links dynamically
    const links = document.querySelectorAll('main a');
    links.forEach(function(link) {
        // Skip links that already have class 'nav-link' or 'dropdown-item'
        if (link.classList.contains('nav-link') || link.classList.contains('dropdown-item')) {
            return;
        }

        const text = link.textContent.trim().toLowerCase();
        
        if (text === 'back to list' || text === 'volver a la lista' || text === 'regresar') {
            link.className = ''; // Clear existing bootstrap classes to avoid interference
            link.classList.add('btn-back-premium');
            link.innerHTML = '<i class="bi bi-arrow-left-circle-fill me-2"></i>Regresar';
            removeSiblingPipes(link);
            wrapInContainerIfNeeded(link);
        } else if (text === 'edit' || text === 'editar') {
            // Only apply premium edit button styling if it's not a table action edit link
            // Table edit links are styled specifically in site.css (.table td:last-child a)
            if (!link.closest('table')) {
                link.className = ''; // Clear existing bootstrap classes to avoid interference
                link.classList.add('btn-edit-premium');
                link.innerHTML = '<i class="bi bi-pencil-square me-2"></i>Editar';
                removeSiblingPipes(link);
                wrapInContainerIfNeeded(link);
            } else {
                // Table edit link: just translate text to Spanish and add icon
                link.innerHTML = '<i class="bi bi-pencil-fill me-1"></i>Editar';
            }
        } else if (text === 'delete' || text === 'eliminar') {
            if (!link.closest('table')) {
                link.className = 'btn btn-danger rounded-pill px-4 py-2 fw-bold shadow-sm m-1';
                link.innerHTML = '<i class="bi bi-trash me-1"></i>Eliminar';
                removeSiblingPipes(link);
            } else {
                link.innerHTML = '<i class="bi bi-trash-fill me-1"></i>Eliminar';
            }
        } else if (text === 'details' || text === 'detalles') {
            if (!link.closest('table')) {
                link.className = 'btn btn-info rounded-pill px-4 py-2 fw-bold shadow-sm m-1';
                link.innerHTML = '<i class="bi bi-info-circle me-1"></i>Detalles';
                removeSiblingPipes(link);
            } else {
                link.innerHTML = '<i class="bi bi-info-circle-fill me-1"></i>Detalles';
            }
        } else if (text === 'create' || text === 'crear' || text === 'create new' || text === 'crear nuevo') {
            link.className = 'btn btn-primary rounded-pill px-4 py-2 fw-bold shadow-sm m-1';
            link.innerHTML = '<i class="bi bi-plus-circle me-1"></i>Crear Nuevo';
            removeSiblingPipes(link);
        }
    });

    function removeSiblingPipes(element) {
        const parent = element.parentElement;
        if (!parent) return;
        
        // Remove text nodes containing "|"
        const childNodes = Array.from(parent.childNodes);
        childNodes.forEach(node => {
            if (node.nodeType === Node.TEXT_NODE && node.nodeValue.indexOf('|') !== -1) {
                node.nodeValue = node.nodeValue.replace(/\|/g, '');
            }
        });
    }

    function wrapInContainerIfNeeded(element) {
        const parent = element.parentElement;
        if (!parent) return;
        
        // Wrap the button or its parent container in btn-container-premium if not already done
        if (!parent.classList.contains('btn-container-premium')) {
            // Check if the parent is a simple div or paragraph that holds these action links
            if (parent.tagName === 'DIV' || parent.tagName === 'P') {
                parent.classList.add('btn-container-premium');
            }
        }
    }
});
