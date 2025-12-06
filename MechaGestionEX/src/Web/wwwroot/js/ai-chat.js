$(document).ready(function () {
    $('#sendChat').click(function () {
        var message = $('#chatInput').val().trim();
        if (!message) return;
        appendMessage('Tú: ' + message, 'user');
        $('#chatInput').val('');

        var loadingId = 'loading-' + Date.now();
        $('#chatHistory').append(`<div id="${loadingId}" class="mb-2 assistant-message">Cargando... <div class="spinner-border spinner-border-sm" role="status"></div></div>`);
        $('#chatHistory').scrollTop($('#chatHistory')[0].scrollHeight);

        $.ajax({
            url: '/ai-assistant/chat',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ userMessage: message }),
            dataType: 'json',
            success: function (response) {
                $(`#${loadingId}`).remove();
                appendMessage('Asistente: ' + response.reply, 'assistant');
            },
            error: function (xhr, status, error) {
                $(`#${loadingId}`).remove();
                console.error('Chat error:', xhr.responseText);
                appendMessage('Error: ' + (xhr.responseJSON?.title || 'No se pudo conectar. Verifica la consola.'), 'system');
            }
        });
    });

    function appendMessage(text, sender) {
        $('#chatHistory').append(`<div class="mb-2 ${sender}-message">${text}</div>`);
        $('#chatHistory').scrollTop($('#chatHistory')[0].scrollHeight);
    }
});