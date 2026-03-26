// ── Sidebar Toggle ────────────────────────────────────────────────────────
const sidebar    = document.getElementById('sidebar');
const mainContent = document.getElementById('mainContent');
const toggleBtn  = document.getElementById('sidebarToggle');

if (toggleBtn && sidebar) {
    toggleBtn.addEventListener('click', () => {
        if (window.innerWidth <= 768) {
            sidebar.classList.toggle('open');
        } else {
            sidebar.classList.toggle('collapsed');
            mainContent?.classList.toggle('expanded');
        }
    });
}

// ── File Input Preview ────────────────────────────────────────────────────
const fileInput   = document.getElementById('fileInput');
const filePreview = document.getElementById('filePreview');

if (fileInput && filePreview) {
    fileInput.addEventListener('change', function () {
        if (this.files && this.files.length > 0) {
            const file = this.files[0];
            const sizeMB = (file.size / 1024 / 1024).toFixed(2);
            filePreview.innerHTML =
                `<div class="alert alert-info py-2 mb-0">
                    <i class="bi bi-paperclip me-2"></i>
                    <strong>${escapeHtml(file.name)}</strong>
                    <span class="text-muted ms-2">(${sizeMB} MB)</span>
                </div>`;
        } else {
            filePreview.innerHTML = '';
        }
    });
}

// ── Status filter auto-submit ─────────────────────────────────────────────
const statusFilter = document.getElementById('statusFilter');
if (statusFilter) {
    statusFilter.addEventListener('change', function () {
        this.closest('form').submit();
    });
}

// ── Confirm before status change ──────────────────────────────────────────
document.querySelectorAll('.btn-status-action').forEach(btn => {
    btn.addEventListener('click', function (e) {
        const action = this.dataset.action;
        const id     = this.dataset.id;
        if (!confirm(`Are you sure you want to ${action} License #${id}?`)) {
            e.preventDefault();
        }
    });
});

function escapeHtml(text) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(text));
    return div.innerHTML;
}
