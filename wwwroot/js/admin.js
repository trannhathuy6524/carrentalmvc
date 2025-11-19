$(document).ready(function () {
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
    });

    // Auto-hide alerts after 5 seconds
    $('.alert').delay(5000).fadeOut(300);

    // Confirm dialogs for dangerous actions
    $('[data-confirm]').click(function (e) {
        if (!confirm($(this).data('confirm'))) {
            e.preventDefault();
            return false;
        }
    });

    // Loading states for buttons
    $('form').on('submit', function () {
        $(this).find('button[type="submit"]').addClass('btn-loading').prop('disabled', true);
    });

    // DataTables initialization (if available)
    if ($.fn.DataTable) {
        $('.datatable').DataTable({
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.4/i18n/vi.json'
            },
            responsive: true,
            pageLength: 25
        });
    }

    // Tooltip initialization
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Popover initialization
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Number formatting
    $('.number-format').each(function () {
        var value = parseInt($(this).text());
        $(this).text(value.toLocaleString('vi-VN'));
    });

    // Currency formatting
    $('.currency-format').each(function () {
        var value = parseFloat($(this).text());
        $(this).text(value.toLocaleString('vi-VN') + ' VNĐ');
    });

    // Status badge animation
    $('.badge').hover(function () {
        $(this).addClass('animate__animated animate__pulse');
    }, function () {
        $(this).removeClass('animate__animated animate__pulse');
    });

    // Search input focus
    $('input[type="search"]').focus(function () {
        $(this).parent().addClass('input-group-focus');
    }).blur(function () {
        $(this).parent().removeClass('input-group-focus');
    });

    // Form validation feedback
    $('input, select, textarea').on('invalid', function () {
        $(this).addClass('is-invalid');
    }).on('input change', function () {
        if (this.validity.valid) {
            $(this).removeClass('is-invalid').addClass('is-valid');
        }
    });

    // Auto-refresh for real-time data (dashboard)
    if (window.location.pathname.includes('/Admin/Dashboard')) {
        setInterval(function () {
            // Refresh specific dashboard elements
            $('.dashboard-stats').each(function () {
                // Update logic here
            });
        }, 30000); // Refresh every 30 seconds
    }

    // Smooth scrolling for anchor links
    $('a[href^="#"]').on('click', function (event) {
        var target = $(this.getAttribute('href'));
        if (target.length) {
            event.preventDefault();
            $('html, body').stop().animate({
                scrollTop: target.offset().top - 100
            }, 1000);
        }
    });

    // Print functionality
    $('.btn-print').click(function () {
        window.print();
    });

    // Export functionality placeholder
    $('.btn-export').click(function () {
        var format = $(this).data('format') || 'excel';
        // Export logic here
        console.log('Exporting to ' + format);
    });

    // Status update with confirmation
    $('.btn-status-update').click(function (e) {
        e.preventDefault();
        var $this = $(this);
        var action = $this.data('action');
        var confirmMessage = $this.data('confirm') || 'Bạn có chắc chắn muốn thực hiện hành động này?';

        if (confirm(confirmMessage)) {
            $this.closest('form').submit();
        }
    });

    // Image preview functionality
    $('input[type="file"][data-preview]').change(function () {
        var input = this;
        var previewId = $(input).data('preview');
        var $preview = $('#' + previewId);

        if (input.files && input.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                $preview.attr('src', e.target.result).show();
            };
            reader.readAsDataURL(input.files[0]);
        }
    });

    // Bulk actions
    $('.select-all').change(function () {
        var isChecked = $(this).is(':checked');
        $('.item-checkbox').prop('checked', isChecked);
        updateBulkActionButtons();
    });

    $('.item-checkbox').change(function () {
        updateBulkActionButtons();

        var totalItems = $('.item-checkbox').length;
        var checkedItems = $('.item-checkbox:checked').length;
        $('.select-all').prop('checked', totalItems === checkedItems);
    });

    function updateBulkActionButtons() {
        var checkedCount = $('.item-checkbox:checked').length;
        if (checkedCount > 0) {
            $('.bulk-actions').removeClass('d-none');
            $('.bulk-count').text(checkedCount);
        } else {
            $('.bulk-actions').addClass('d-none');
        }
    }

    // Real-time search
    let searchTimeout;
    $('.real-time-search').on('input', function () {
        clearTimeout(searchTimeout);
        var $input = $(this);
        var query = $input.val();

        searchTimeout = setTimeout(function () {
            if (query.length >= 2 || query.length === 0) {
                performRealTimeSearch(query, $input.data('target'));
            }
        }, 500);
    });

    function performRealTimeSearch(query, target) {
        // Implementation for real-time search
        console.log('Searching for: ' + query + ' in ' + target);
    }
});

// Utility functions
function showAlert(message, type = 'info') {
    var alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <i class="fas fa-${getAlertIcon(type)}"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    $('.content-wrapper').prepend(alertHtml);
    setTimeout(function () {
        $('.alert').first().fadeOut(300);
    }, 5000);
}

function getAlertIcon(type) {
    switch (type) {
        case 'success': return 'check-circle';
        case 'danger': return 'exclamation-triangle';
        case 'warning': return 'exclamation-triangle';
        case 'info': return 'info-circle';
        default: return 'info-circle';
    }
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0
    }).format(amount);
}

function formatNumber(number) {
    return new Intl.NumberFormat('vi-VN').format(number);
}

function formatDate(date) {
    return new Date(date).toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// Chart utilities
function createLineChart(ctx, data, options = {}) {
    return new Chart(ctx, {
        type: 'line',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            ...options
        }
    });
}

function createDoughnutChart(ctx, data, options = {}) {
    return new Chart(ctx, {
        type: 'doughnut',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            ...options
        }
    });
}

function createBarChart(ctx, data, options = {}) {
    return new Chart(ctx, {
        type: 'bar',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            ...options
        }
    });
}

// Ajax helpers
function ajaxGet(url, success, error) {
    $.ajax({
        url: url,
        type: 'GET',
        success: success || function (data) {
            console.log('Success:', data);
        },
        error: error || function (xhr, status, err) {
            console.error('Error:', err);
            showAlert('Có lỗi xảy ra khi tải dữ liệu', 'danger');
        }
    });
}

function ajaxPost(url, data, success, error) {
    $.ajax({
        url: url,
        type: 'POST',
        data: data,
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: success || function (response) {
            console.log('Success:', response);
            showAlert('Thao tác thành công', 'success');
        },
        error: error || function (xhr, status, err) {
            console.error('Error:', err);
            showAlert('Có lỗi xảy ra khi thực hiện thao tác', 'danger');
        }
    });
}

// Local storage helpers
function saveToStorage(key, value) {
    try {
        localStorage.setItem(key, JSON.stringify(value));
    } catch (e) {
        console.warn('Cannot save to localStorage:', e);
    }
}

function loadFromStorage(key, defaultValue = null) {
    try {
        const item = localStorage.getItem(key);
        return item ? JSON.parse(item) : defaultValue;
    } catch (e) {
        console.warn('Cannot load from localStorage:', e);
        return defaultValue;
    }
}

// Modal helpers
function showModal(modalId, title, content) {
    var $modal = $('#' + modalId);
    if ($modal.length) {
        $modal.find('.modal-title').text(title);
        $modal.find('.modal-body').html(content);
        $modal.modal('show');
    }
}

function hideModal(modalId) {
    $('#' + modalId).modal('hide');
}

// Progress bar helpers
function updateProgress(progressId, percentage) {
    var $progress = $('#' + progressId);
    $progress.css('width', percentage + '%')
        .attr('aria-valuenow', percentage)
        .text(percentage + '%');
}

// Toast notifications
function showToast(message, type = 'info', duration = 3000) {
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-${getAlertIcon(type)} me-2"></i>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    let $toastContainer = $('.toast-container');
    if (!$toastContainer.length) {
        $toastContainer = $('<div class="toast-container position-fixed bottom-0 end-0 p-3"></div>');
        $('body').append($toastContainer);
    }

    const $toast = $(toastHtml);
    $toastContainer.append($toast);

    const toast = new bootstrap.Toast($toast[0], {
        delay: duration
    });
    toast.show();

    $toast.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

// File upload helpers
function validateFileType(file, allowedTypes) {
    return allowedTypes.includes(file.type);
}

function validateFileSize(file, maxSizeInMB) {
    const maxSizeInBytes = maxSizeInMB * 1024 * 1024;
    return file.size <= maxSizeInBytes;
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Debounce function
function debounce(func, wait, immediate) {
    var timeout;
    return function () {
        var context = this, args = arguments;
        var later = function () {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };
        var callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func.apply(context, args);
    };
}

// Throttle function
function throttle(func, limit) {
    var inThrottle;
    return function () {
        var args = arguments;
        var context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    }
}