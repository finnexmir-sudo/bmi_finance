// ── User Chat JS (SignalR) ────────────────────────────────

(function () {
    'use strict';

    var menimIsciId = 0;
    var secilmisIsciId = 0;

    // ── SignalR Connection ───────────────────────────────
    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveMessage", function (data) {
        // If this message is from the currently selected contact, append it
        if (data.gonderenIsciId === secilmisIsciId) {
            appendMessage(data.metn, data.tarix, false);
            scrollToBottom();
            // Mark as read
            connection.invoke("MarkAsRead", data.gonderenIsciId).catch(function () { });
        }
        // Update unread badge
        updateContactUnread(data.gonderenIsciId);
    });

    connection.on("MessageSent", function (data) {
        // Confirmation of sent message - already appended locally
    });

    connection.start()
        .then(function () { console.log('SignalR bağlandı'); })
        .catch(function (err) {
            console.error('SignalR bağlantı xətası:', err);
        });

    // ── Load Contacts ───────────────────────────────────
    function loadContacts() {
        fetch('/User/Chat/GetContacts')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                menimIsciId = data.menimIsciId;
                document.getElementById('menimIsciId').value = menimIsciId;
                renderContacts(data.contacts);
            })
            .catch(function () {
                document.getElementById('contactList').innerHTML =
                    '<div class="chat-loading">Xeta bash verdi</div>';
            });
    }

    function renderContacts(contacts) {
        var list = document.getElementById('contactList');

        if (!contacts || contacts.length === 0) {
            list.innerHTML = '<div class="chat-loading">Kontakt tapilmadi</div>';
            return;
        }

        var html = '';
        contacts.forEach(function (c) {
            var initials = (c.ad ? c.ad[0] : '') + (c.soyad ? c.soyad[0] : '');
            html += '<div class="chat-contact" data-isci-id="' + c.isciId + '" data-ad="' + escHtml(c.ad + ' ' + c.soyad) + '">';
            html += '<div class="chat-contact-avatar">' + initials.toUpperCase() + '</div>';
            html += '<div class="chat-contact-info">';
            html += '<div class="chat-contact-name">' + escHtml(c.ad + ' ' + c.soyad) + '</div>';
            html += '</div>';
            if (c.oxunmamis > 0) {
                html += '<div class="chat-contact-unread" data-unread-id="' + c.isciId + '">' + c.oxunmamis + '</div>';
            }
            html += '</div>';
        });

        list.innerHTML = html;

        // Attach click events
        var items = list.querySelectorAll('.chat-contact');
        items.forEach(function (item) {
            item.addEventListener('click', function () {
                var isciId = parseInt(this.dataset.isciId);
                var ad = this.dataset.ad;
                selectContact(isciId, ad, this);
            });
        });
    }

    // ── Select Contact ──────────────────────────────────
    function selectContact(isciId, ad, el) {
        secilmisIsciId = isciId;
        document.getElementById('secilmisIsciId').value = isciId;

        // Highlight
        document.querySelectorAll('.chat-contact').forEach(function (c) {
            c.classList.remove('chat-contact--active');
        });
        if (el) el.classList.add('chat-contact--active');

        // Show header
        var header = document.getElementById('chatHeader');
        header.style.display = 'flex';
        document.getElementById('chatName').textContent = ad;
        var initials = ad.split(' ').map(function (w) { return w[0] || ''; }).join('').substring(0, 2);
        document.getElementById('chatAvatar').textContent = initials.toUpperCase();

        // Show input
        document.getElementById('chatInputArea').style.display = 'flex';

        // Remove unread badge
        var badge = document.querySelector('[data-unread-id="' + isciId + '"]');
        if (badge) badge.remove();

        // Load messages
        loadMessages(isciId);
    }

    // ── Load Messages ───────────────────────────────────
    function loadMessages(isciId) {
        var container = document.getElementById('chatMessages');
        container.innerHTML = '<div class="chat-loading">Yuklenir...</div>';

        fetch('/User/Chat/GetMessages?isciId=' + isciId)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                renderMessages(data.mesajlar);
            })
            .catch(function () {
                container.innerHTML = '<div class="chat-loading">Xeta bash verdi</div>';
            });
    }

    function renderMessages(mesajlar) {
        var container = document.getElementById('chatMessages');

        if (!mesajlar || mesajlar.length === 0) {
            container.innerHTML = '<div class="chat-empty-state"><div class="chat-empty-title">Hele mesaj yoxdur</div><div class="chat-empty-sub">Ilk mesaji gonderin</div></div>';
            return;
        }

        var html = '';
        var lastDate = '';

        mesajlar.forEach(function (m) {
            var msgDate = m.tarix.split(' ')[0];
            if (msgDate !== lastDate) {
                html += '<div class="chat-date-sep">' + msgDate + '</div>';
                lastDate = msgDate;
            }

            var cls = m.menimdir ? 'chat-msg--mine' : 'chat-msg--other';
            html += '<div class="chat-msg ' + cls + '">';
            html += '<div>' + escHtml(m.metn) + '</div>';
            html += '<div class="chat-msg-time">' + m.saatStr + '</div>';
            html += '</div>';
        });

        container.innerHTML = html;
        scrollToBottom();
    }

    function appendMessage(metn, tarix, isMine) {
        var container = document.getElementById('chatMessages');
        // Remove empty state if present
        var empty = container.querySelector('.chat-empty-state');
        if (empty) empty.remove();

        var cls = isMine ? 'chat-msg--mine' : 'chat-msg--other';
        var div = document.createElement('div');
        div.className = 'chat-msg ' + cls;
        div.innerHTML = '<div>' + escHtml(metn) + '</div><div class="chat-msg-time">' + tarix + '</div>';
        container.appendChild(div);
    }

    function scrollToBottom() {
        var container = document.getElementById('chatMessages');
        container.scrollTop = container.scrollHeight;
    }

    // ── Send Message ────────────────────────────────────
    function sendMessage() {
        var input = document.getElementById('chatInput');
        var metn = input.value.trim();

        if (!metn || !secilmisIsciId) return;

        // Append locally immediately
        var now = new Date();
        var saatStr = ('0' + now.getHours()).slice(-2) + ':' + ('0' + now.getMinutes()).slice(-2);
        appendMessage(metn, saatStr, true);
        scrollToBottom();
        input.value = '';

        // Send via SignalR
        connection.invoke("SendMessage", secilmisIsciId, metn).catch(function (err) {
            console.error('Mesaj gonderme xetasi:', err);
        });
    }

    // ── Update unread badge ─────────────────────────────
    function updateContactUnread(isciId) {
        if (isciId === secilmisIsciId) return;

        var badge = document.querySelector('[data-unread-id="' + isciId + '"]');
        if (badge) {
            var count = parseInt(badge.textContent) + 1;
            badge.textContent = count;
        } else {
            var contact = document.querySelector('[data-isci-id="' + isciId + '"]');
            if (contact) {
                var newBadge = document.createElement('div');
                newBadge.className = 'chat-contact-unread';
                newBadge.setAttribute('data-unread-id', isciId);
                newBadge.textContent = '1';
                contact.appendChild(newBadge);
            }
        }
    }

    // ── Search Contacts ─────────────────────────────────
    function filterContacts() {
        var term = document.getElementById('contactSearch').value.toLowerCase();
        var items = document.querySelectorAll('.chat-contact');
        items.forEach(function (item) {
            var name = item.dataset.ad.toLowerCase();
            item.style.display = name.includes(term) ? '' : 'none';
        });
    }

    // ── Helpers ─────────────────────────────────────────
    function escHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // ── Event Listeners ─────────────────────────────────
    document.getElementById('btnSend').addEventListener('click', sendMessage);

    document.getElementById('chatInput').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            sendMessage();
        }
    });

    document.getElementById('contactSearch').addEventListener('input', filterContacts);

    // Init
    loadContacts();

})();
