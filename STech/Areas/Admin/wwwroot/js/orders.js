﻿const loadOrders = (page, filer_by, sort_by) => {
    updateParams({
        page: page,
        filter_by: filer_by,
        sort_by: sort_by
    });

    hideWebLoader(0);
    showWebLoader();

    $.ajax({
        url: `/api/admin/orders?page=${page}&filter_by=${filer_by}&sort_by=${sort_by}`,
        type: 'GET',
        success: (response) => {
            hideWebLoader(500);

            if (response.status) {
                $('.order-list').empty().append(`
                    <tr class="page-table-header">
                        <th>STT</th>
                        <th>Mã đơn hàng</th>
                        <th>Ngày đặt</th>
                        <th>Số SP</th>
                        <th>Thành tiền</th>
                        <th>PT thanh toán</th>
                        <th>TT thanh toán</th>
                        <th>Trạng thái</th>
                        <th></th>
                    </tr>
                `);

                loadPagination(response.data.totalPages, response.data.currentPage);

                let index = 0;

                response.data.invoices.forEach((invoice) => {
                    index++;

                    let payment_status = ``;
                    let order_status = ``;

                    const order_date = new Date(invoice.orderDate);
                    const day = String(order_date.getDate()).padStart(2, '0');
                    const month = String(order_date.getMonth() + 1).padStart(2, '0');
                    const year = order_date.getFullYear();
                    const hours = String(order_date.getHours()).padStart(2, '0');
                    const minutes = String(order_date.getMinutes()).padStart(2, '0');

                    const total_items = invoice.invoiceDetails.reduce((accumulator, item) => {
                        return accumulator + item.quantity;
                    }, 0);

                    switch (invoice.paymentStatus) { 
                        case "paid":
                            payment_status = '<span class="page-badge badge-success">Đã thanh toán</span>';
                            break;

                        case "unpaid":
                            payment_status = '<span class="page-badge badge-warning">Chờ thanh toán</span>';
                            break;

                        case "payment-failed":
                            payment_status = '<span class="page-badge badge-error">Thanh toán thất bại</span>';
                            break;

                    }

                    if (invoice.isCancelled) {
                        order_status = '<span class="page-badge badge-error">Đã hủy</span>';
                    }
                    else if (invoice.isCompleted) {
                        order_status = '<span class="page-badge badge-success">Đã giao hàng</span>';
                    }
                    else if (invoice.isAccepted) {
                        order_status = '<span class="page-badge badge-warning">Chờ giao hàng</span>';
                    }
                    else {
                        order_status = '<span class="page-badge badge-warning">Chờ xác nhận</span>';
                    }

                    $('.order-list').append(`
                        <tr>
                            <td class="fweight-600">${index}</td>
                            <td>${invoice.invoiceId}</td>
                            <td>${day}/${month}/${year} ${hours}:${minutes}</td>
                            <td>${total_items}</td>
                            <td class="fweight-600">${invoice.total.toLocaleString('vi-VN')}đ</td>
                            <td>${invoice.paymentMed.paymentName}</td>
                            <td>${payment_status}</td>
                            <td>${order_status}</td>
                            <td>
                                <div class="d-flex gap-1 align-items-center justify-content-end">
                                    <button class="page-table-btn btn-lightblue view-order" data-order="${invoice.invoiceId}">
                                        <i class="fa-solid fa-eye"></i>
                                    </button>
                                    <button class="page-table-btn btn-blue print-order" data-order="${invoice.invoiceId}">
                                        <i class="fa-solid fa-print"></i>
                                    </button>
                                    ${!invoice.isAccepted && !invoice.isCancelled ?
                                        `<button class="page-table-btn btn-green accept-order" data-order="${invoice.invoiceId}">
                                            <i class="fa-solid fa-check"></i>
                                        </button>` : ''}
                                </div>
                            </td>
                        </tr>
                    `);
                });
            }
        },
        error: () => {
            hideWebLoader(0);
            showDialog('error', 'Đã xảy ra lỗi', 'Không thể tải danh sách đơn hàng');
        }
    })
}

$('.orders-nav').click(function () {
    const filter_by = $(this).data('load-orders') ?? '';
    loadOrders(1, filter_by, '');
})

$(document).on('click', '.pagination-item:not(.current)', function () {
    const page = $(this).data('page') || '1';
    const current_page = parseInt($('.pagination-item.current').data('page') || '1');
    const filter_by = $('.orders-nav.active').data('load-orders') || '';

    if (page === 'next') {
        loadOrders(current_page + 1, filter_by, '');
    } else if (page === 'previous') {
        loadOrders(current_page - 1, filter_by, '');
    } else {
        loadOrders(page, filter_by, '');
    }
})


tippy('.view-order', {
    content: 'Xem chi tiết',
    placement: 'top'
})

tippy('.print-order', {
    content: 'In hóa đơn',
    placement: 'top'
})

tippy('.accept-order', {
    content: 'Xác nhận đơn hàng',
    placement: 'top'
})

$(document).on('click', '.view-order', function () { 

})

$(document).on('click', '.accept-order', function () {
    showConfirmDialog('Xác nhận đơn hàng', 'Bạn có chắc chắn muốn xác nhận đơn hàng này không?', () => {
        const current_page = parseInt($('.pagination-item.current').data('page') || '1');
        const filter_by = $('.orders-nav.active').data('load-orders') || '';
        const order_id = $(this).data('order');

        if (order_id) {
            showWebLoader();

            $.ajax({
                type: 'PATCH',
                url: `/api/admin/orders/accept/${order_id}`,
                success: (response) => {
                    hideWebLoader();

                    if (response.status) {
                        loadOrders(current_page, filter_by, '');
                        showDialog('success', 'Thành công', 'Đã xác nhận đơn hàng');
                    } else {
                        showDialog('error', 'Đã xảy ra lỗi', response.message);
                    }
                },
                error: () => {
                    hideWebLoader(0);
                    showErrorDialog();
                }
            });
        }
    });
})

$(document).on('click', '.print-order', async function () {
    const order_id = $(this).data('order');

    if (order_id) {
        try {
            const response = await fetch(`/api/admin/orders/print-invoice/${order_id}`);

            if (!response.ok) {
                throw new Error('Đã xảy ra lỗi');
            }

            const blob = await response.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');

            a.style.display = 'none';
            a.href = url;
            a.download = `HĐ_${order_id}.pdf`;

            document.body.appendChild(a);
            a.click();

            URL.revokeObjectURL(url);
            document.body.removeChild(a);
        } catch (error) {
            showDialog('error', 'Không thể tải hóa đơn', error);
        }
    }
})