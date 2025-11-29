$(document).ready(function () {

    $('#tipo').select2();

    $('#tipo_servicio_id').select2({
        placeholder: 'Seleccione un tipo de servicio',
        allowClear: true,
        dropdownParent: $('#agregarServicioModal'),
        ajax: {
            url: '/Atencion/GetTiposServicio',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return {
                    search: params.term
                };
            },
            processResults: function (data) {
                return {
                    results: data
                };
            },
            cache: true
        },
        minimumInputLength: 0
    });

    $('#agregarServicioModal').on('show.bs.modal', function () {
        $('#agregarServicioForm')[0].reset();
        $('#tipo_servicio_id').val(null).trigger('change');
        $('#repuestos-container').empty();
        agregarFilaRepuesto();
        calcularTotals();
    });

    $('#agregarRepuestoBtn').click(function () {
        agregarFilaRepuesto();
    });

    $('#precio_mano_obra').on('input', calcularTotals);

    function agregarFilaRepuesto() {
        var index = $('#repuestos-container .repuesto-row').length;
        var html = `
            <div class="repuesto-row mb-2" data-index="${index}">
                <div class="input-group">
                    <select class="form-select repuesto-select" name="repuestos[${index}].repuesto_unidades_id" data-index="${index}"></select>
                    <input type="number" class="form-control cantidad-input d-none" name="repuestos[${index}].cantidad" min="1" value="1" placeholder="Cantidad" />
                    <button type="button" class="btn btn-outline-danger eliminar-repuesto d-none"><i class="bi bi-trash"></i></button>
                </div>
                <div class="repuesto-info mt-1 d-none"></div>
            </div>
        `;
        $('#repuestos-container').append(html);

        var $select = $(`.repuesto-select[data-index="${index}"]`);
        $select.select2({
            placeholder: 'Seleccione un repuesto',
            allowClear: true,
            dropdownParent: $('#agregarServicioModal'),
            ajax: {
                url: '/Atencion/GetRepuestosPorTaller',
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return {
                        search: params.term
                    };
                },
                processResults: function (data) {
                    return {
                        results: data
                    };
                },
                cache: true
            },
            minimumInputLength: 0,
            templateResult: function (item) {
                return item.text;
            },
            templateSelection: function (item) {
                return item.text;
            }
        });

        $select.on('select2:select', function (e) {
            var data = e.params.data;
            var $row = $(this).closest('.repuesto-row');
            $row.find('.cantidad-input').removeClass('d-none').attr('max', data.stock);
            $row.find('.eliminar-repuesto').removeClass('d-none');
            $row.find('.repuesto-info').html(`SKU: ${data.text.split(' - ')[0]} | Nombre: ${data.text.split(' - ')[1].split(' (')[0]} | Stock: ${data.stock} | Precio: $${data.precio}`).removeClass('d-none');
            $row.data('precio', data.precio);
            calcularTotals();
        });

        $select.on('select2:clear', function () {
            var $row = $(this).closest('.repuesto-row');
            $row.find('.cantidad-input, .eliminar-repuesto, .repuesto-info').addClass('d-none');
            $row.removeData('precio');
            calcularTotals();
        });

        $('.repuesto-row[data-index="' + index + '"]').find('.cantidad-input').on('input', function () {
            var val = parseInt($(this).val());
            var max = parseInt($(this).attr('max'));
            if (val > max) $(this).val(max);
            if (val < 1) $(this).val(1);
            calcularTotals();
        });

        $('.repuesto-row[data-index="' + index + '"]').find('.eliminar-repuesto').click(function () {
            $(this).closest('.repuesto-row').remove();
            calcularTotals();
        });
    }

    function calcularTotals() {
        var totalRepuestos = 0;
        $('.repuesto-row').each(function () {
            var precio = $(this).data('precio') || 0;
            var cantidad = parseInt($(this).find('.cantidad-input').val()) || 0;
            totalRepuestos += precio * cantidad;
        });
        $('#total_repuestos_display').text(totalRepuestos.toLocaleString());

        var manoObra = parseInt($('#precio_mano_obra').val()) || 0;
        var subTotal = manoObra + totalRepuestos;
        $('#sub_total_display').text(subTotal.toLocaleString());
        $('#sub_total').val(subTotal);
    }

    $('#guardarServicioBtn').click(function () {
        if (!$('#tipo_servicio_id').val()) {
            alert('Seleccione un tipo de servicio.');
            return;
        }

        var formData = $('#agregarServicioForm').serializeArray();

        $.ajax({
            url: '/Atencion/AgregarServicio',
            type: 'POST',
            data: formData,
            headers: {
                RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    var servicio = response.servicio;
                    var html = `
                        <li class="list-group-item">
                            <div class="d-flex justify-content-between">
                                <strong>${servicio.tipo_nombre}</strong>
                                <span>Subtotal: $${servicio.sub_total.toLocaleString()}</span>
                            </div>
                            <p class="text-muted">${servicio.descripcion || ''}</p>
                    `;
                    if (servicio.repuestos.length > 0) {
                        html += `<ul class="list-group list-group-flush mt-2">`;
                        servicio.repuestos.forEach(function (rep) {
                            html += `
                                <li class="list-group-item small">
                                    ${rep.sku} - ${rep.nombre} (Cantidad: ${rep.cantidad}, Stock Disponible: ${rep.stock_disponible})
                                </li>
                            `;
                        });
                        html += `</ul>`;
                    }
                    html += `</li>`;
                    $('.list-group').append(html);
                    $('.text-muted:contains("Aún no se han agregado")').remove();
                    $('#agregarServicioModal').modal('hide');
                } else {
                    alert(response.message || 'Error al agregar servicio.');
                }
            },
            error: function () {
                alert('Error en la solicitud.');
            }
        });
    });
});