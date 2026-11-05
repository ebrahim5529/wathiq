/**
 * Wathiq Archive System - Confirmation & Toast Notification Library
 * Uses SweetAlert2 with RTL support & seamless Bootstrap 5 Modal offline fallback
 */
(function () {
    'use strict';

    // 1. Unified Toast Notification for CRUD feedback
    window.wathiqToast = function (type, message, title) {
        if (!message) return;

        type = (type || 'success').toLowerCase();
        if (type === 'danger') type = 'error';

        if (typeof Swal !== 'undefined') {
            const Toast = Swal.mixin({
                toast: true,
                position: 'top-start',
                showConfirmButton: false,
                timer: 4000,
                timerProgressBar: true,
                didOpen: (toast) => {
                    toast.addEventListener('mouseenter', Swal.stopTimer);
                    toast.addEventListener('mouseleave', Swal.resumeTimer);
                }
            });

            Toast.fire({
                icon: type,
                title: title || (type === 'success' ? 'تمت العملية بنجاح' : (type === 'error' ? 'تنبيه' : 'إشعار')),
                text: message
            });
        }
    };

    // 2. Fallback Bootstrap 5 Confirmation Dialog
    function showBootstrapConfirm(options) {
        return new Promise(function (resolve) {
            let modalEl = document.getElementById('wathiqFallbackConfirmModal');
            if (!modalEl) {
                const markup = `
                    <div class="modal fade" id="wathiqFallbackConfirmModal" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
                        <div class="modal-dialog modal-dialog-centered modal-sm">
                            <div class="modal-content shadow-lg border-0">
                                <div class="modal-body text-center p-4">
                                    <div class="mb-3" id="wathiqFallbackIcon"></div>
                                    <h5 class="fw-bold mb-2" id="wathiqFallbackTitle">تأكيد</h5>
                                    <p class="text-secondary small mb-4" id="wathiqFallbackText"></p>
                                    <div class="d-flex justify-content-center gap-2">
                                        <button type="button" class="btn btn-secondary px-3" data-bs-dismiss="modal" id="wathiqFallbackCancelBtn">إلغاء</button>
                                        <button type="button" class="btn btn-danger px-3" id="wathiqFallbackOkBtn">تأكيد</button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>`;
                document.body.insertAdjacentHTML('beforeend', markup);
                modalEl = document.getElementById('wathiqFallbackConfirmModal');
            }

            const titleEl = document.getElementById('wathiqFallbackTitle');
            const textEl = document.getElementById('wathiqFallbackText');
            const okBtn = document.getElementById('wathiqFallbackOkBtn');
            const cancelBtn = document.getElementById('wathiqFallbackCancelBtn');
            const iconEl = document.getElementById('wathiqFallbackIcon');

            titleEl.textContent = options.title || 'تأكيد العملية';
            textEl.textContent = options.text || 'هل أنت متأكد من الاستمرار؟';
            okBtn.textContent = options.confirmButtonText || 'نعم، متابعة';
            cancelBtn.textContent = options.cancelButtonText || 'إلغاء';

            const isDanger = options.confirmButtonColor === '#dc3545' || options.isDanger !== false;
            okBtn.className = 'btn ' + (isDanger ? 'btn-danger' : 'btn-primary') + ' px-3';

            if (options.icon === 'warning') {
                iconEl.innerHTML = '<i class="bi bi-exclamation-triangle-fill display-4 text-warning"></i>';
            } else if (options.icon === 'question') {
                iconEl.innerHTML = '<i class="bi bi-question-circle-fill display-4 text-info"></i>';
            } else {
                iconEl.innerHTML = '<i class="bi bi-info-circle-fill display-4 text-primary"></i>';
            }

            const bsModal = new bootstrap.Modal(modalEl);

            let confirmed = false;
            function onOk() {
                confirmed = true;
                bsModal.hide();
            }

            okBtn.onclick = onOk;
            modalEl.addEventListener('hidden.bs.modal', function handler() {
                modalEl.removeEventListener('hidden.bs.modal', handler);
                resolve({ isConfirmed: confirmed });
            });

            bsModal.show();
        });
    }

    // 3. Unified Confirmation Dialog (Promise-based)
    window.wathiqConfirm = function (options) {
        options = options || {};
        const title = options.title || 'هل أنت متأكد؟';
        const text = options.text || 'لن تتمكن من التراجع عن هذه العملية بسهولة!';
        const icon = options.icon || 'warning';
        const confirmBtn = options.confirmButtonText || 'نعم، احذف';
        const cancelBtn = options.cancelButtonText || 'إلغاء الأمر';
        const isDanger = options.isDanger !== false;
        const confirmColor = options.confirmButtonColor || (isDanger ? '#dc3545' : '#0072ce');
        const cancelColor = options.cancelButtonColor || '#6c757d';

        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                title: title,
                text: text,
                icon: icon,
                showCancelButton: true,
                confirmButtonColor: confirmColor,
                cancelButtonColor: cancelColor,
                confirmButtonText: confirmBtn,
                cancelButtonText: cancelBtn,
                reverseButtons: true,
                focusCancel: true,
                backdrop: true
            });
        } else {
            return showBootstrapConfirm({
                title: title,
                text: text,
                icon: icon,
                confirmButtonText: confirmBtn,
                cancelButtonText: cancelBtn,
                confirmButtonColor: confirmColor,
                isDanger: isDanger
            });
        }
    };

    // 4. Global Declarative Handlers for CRUD Confirmations
    document.addEventListener('DOMContentLoaded', function () {
        // Read TempData Toast notifications from DOM
        const toastData = document.getElementById('wathiqCrudToastData');
        if (toastData) {
            const successMsg = toastData.getAttribute('data-success');
            const errorMsg = toastData.getAttribute('data-error');
            const warningMsg = toastData.getAttribute('data-warning');

            if (successMsg) {
                window.wathiqToast('success', successMsg, 'نجاح العملية');
            } else if (errorMsg) {
                window.wathiqToast('error', errorMsg, 'خطأ في العملية');
            } else if (warningMsg) {
                window.wathiqToast('warning', warningMsg, 'تنبيه');
            }
        }

        // Intercept form submission with data-confirm
        document.addEventListener('submit', function (e) {
            const form = e.target;
            if (form.getAttribute('data-confirmed') === 'true') {
                form.removeAttribute('data-confirmed');
                return;
            }

            const confirmMessage = form.getAttribute('data-confirm');
            if (!confirmMessage) return;

            e.preventDefault();
            e.stopImmediatePropagation();

            const title = form.getAttribute('data-confirm-title') || 'تأكيد العملية';
            const confirmBtn = form.getAttribute('data-confirm-btn') || 'نعم، متابعة';
            const cancelBtn = form.getAttribute('data-cancel-btn') || 'إلغاء';
            const icon = form.getAttribute('data-confirm-icon') || 'warning';
            const isDanger = form.getAttribute('data-confirm-danger') !== 'false';

            window.wathiqConfirm({
                title: title,
                text: confirmMessage,
                icon: icon,
                confirmButtonText: confirmBtn,
                cancelButtonText: cancelBtn,
                isDanger: isDanger
            }).then(function (result) {
                if (result.isConfirmed) {
                    form.setAttribute('data-confirmed', 'true');
                    form.submit();
                }
            });
        }, true);

        // Intercept button/link click with data-confirm
        document.addEventListener('click', function (e) {
            const btn = e.target.closest('[data-confirm]');
            if (!btn) return;

            // If the element is a form itself or inside a form that handles it, let submit handle it
            const form = btn.closest('form');
            if (form && btn.type === 'submit' && !form.getAttribute('data-confirm')) {
                if (form.getAttribute('data-confirmed') === 'true') {
                    return;
                }
                e.preventDefault();
                e.stopPropagation();

                const title = btn.getAttribute('data-confirm-title') || 'تأكيد العملية';
                const msg = btn.getAttribute('data-confirm');
                const confirmBtn = btn.getAttribute('data-confirm-btn') || 'نعم، تابع';
                const cancelBtn = btn.getAttribute('data-cancel-btn') || 'إلغاء';
                const icon = btn.getAttribute('data-confirm-icon') || 'warning';
                const isDanger = btn.getAttribute('data-confirm-danger') !== 'false';

                window.wathiqConfirm({
                    title: title,
                    text: msg,
                    icon: icon,
                    confirmButtonText: confirmBtn,
                    cancelButtonText: cancelBtn,
                    isDanger: isDanger
                }).then(function (result) {
                    if (result.isConfirmed) {
                        form.setAttribute('data-confirmed', 'true');
                        // Trigger form submission
                        form.submit();
                    }
                });
            } else if (btn.tagName === 'A' && btn.href && btn.href !== '#' && !btn.href.startsWith('javascript:')) {
                e.preventDefault();
                const msg = btn.getAttribute('data-confirm');
                const title = btn.getAttribute('data-confirm-title') || 'تأكيد الانتقال';
                window.wathiqConfirm({
                    title: title,
                    text: msg,
                    icon: 'warning',
                    confirmButtonText: 'نعم، متابعة',
                    cancelButtonText: 'إلغاء'
                }).then(function (result) {
                    if (result.isConfirmed) {
                        window.location.href = btn.href;
                    }
                });
            }
        }, true);
    });
})();

