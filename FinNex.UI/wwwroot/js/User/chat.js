// ── User Chat JS (AJAX Polling) ───────────────────────────

(function () {
    'use strict';

    var menimIsciId = 0;
    var secilmisIsciId = 0;
    var pollTimer = null;
    var lastMesajId = 0;

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
                    '<div class="chat-loading">Xəta baş verdi</div>';
            });
    }

    function renderContacts(contacts) {
        var list = document.getElementById('contactList');

        if (!contacts || contacts.length === 0) {
            list.innerHTML = '<div class="chat-loading">Kontakt tapılmadı</div>';
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

        list.querySelectorAll('.chat-contact').forEach(function (item) {
            item.addEventListener('click', function () {
                selectContact(parseInt(this.dataset.isciId), this.dataset.ad, this);
            });
        });
    }

    // ── Select Contact ──────────────────────────────────
    function selectContact(isciId, ad, el) {
        secilmisIsciId = isciId;
        document.getElementById('secilmisIsciId').value = isciId;

        document.querySelectorAll('.chat-contact').forEach(function (c) {
            c.classList.remove('chat-contact--active');
        });
        if (el) el.classList.add('chat-contact--active');

        document.getElementById('chatHeader').style.display = 'flex';
        document.getElementById('chatName').textContent = ad;
        var initials = ad.split(' ').map(function (w) { return w[0] || ''; }).join('').substring(0, 2);
        document.getElementById('chatAvatar').textContent = initials.toUpperCase();
        document.getElementById('chatInputArea').style.display = 'flex';

        var badge = document.querySelector('[data-unread-id="' + isciId + '"]');
        if (badge) badge.remove();

        lastMesajId = 0;
        loadMessages(isciId);
        startPolling();
    }

    // ── Load Messages ───────────────────────────────────
    function loadMessages(isciId) {
        var container = document.getElementById('chatMessages');
        container.innerHTML = '<div class="chat-loading" style="color:#94a3b8">Yüklənir...</div>';

        fetch('/User/Chat/GetMessages?isciId=' + isciId)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                renderMessages(data.mesajlar);
                if (data.mesajlar && data.mesajlar.length > 0) {
                    lastMesajId = data.mesajlar[data.mesajlar.length - 1].id;
                }
            })
            .catch(function () {
                container.innerHTML = '<div class="chat-loading" style="color:#dc2626">Xəta baş verdi</div>';
            });
    }

    function renderMessages(mesajlar) {
        var container = document.getElementById('chatMessages');

        if (!mesajlar || mesajlar.length === 0) {
            container.innerHTML = '<div class="chat-empty-state"><div class="chat-empty-title">Hələ mesaj yoxdur</div><div class="chat-empty-sub">İlk mesajı göndərin</div></div>';
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
        var empty = container.querySelector('.chat-empty-state');
        if (empty) empty.remove();

        var cls = isMine ? 'chat-msg--mine' : 'chat-msg--other';
        var div = document.createElement('div');
        div.className = 'chat-msg ' + cls;
        div.innerHTML = '<div>' + escHtml(metn) + '</div><div class="chat-msg-time">' + tarix + '</div>';
        container.appendChild(div);
    }

    function scrollToBottom() {
        var c = document.getElementById('chatMessages');
        c.scrollTop = c.scrollHeight;
    }

    // ── Send Message (AJAX POST) ────────────────────────
    function sendMessage() {
        var input = document.getElementById('chatInput');
        var metn = input.value.trim();
        if (!metn || !secilmisIsciId) return;

        var now = new Date();
        var saatStr = ('0' + now.getHours()).slice(-2) + ':' + ('0' + now.getMinutes()).slice(-2);
        appendMessage(metn, saatStr, true);
        scrollToBottom();
        input.value = '';

        fetch('/User/Chat/Send', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ alanIsciId: secilmisIsciId, metn: metn })
        }).catch(function (err) {
            console.error('Mesaj göndərmə xətası:', err);
        });
    }

    // ── Polling: hər 3 saniyədə yeni mesaj yoxla ────────
    function startPolling() {
        if (pollTimer) clearInterval(pollTimer);
        pollTimer = setInterval(function () {
            if (!secilmisIsciId) return;
            fetch('/User/Chat/GetMessages?isciId=' + secilmisIsciId)
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (!data.mesajlar || data.mesajlar.length === 0) return;
                    var sonMesaj = data.mesajlar[data.mesajlar.length - 1];
                    if (sonMesaj.id > lastMesajId) {
                        // Yeni mesajlar var
                        var yeniMesajlar = data.mesajlar.filter(function (m) { return m.id > lastMesajId; });
                        yeniMesajlar.forEach(function (m) {
                            if (!m.menimdir) {
                                appendMessage(m.metn, m.saatStr, false);
                            }
                        });
                        lastMesajId = sonMesaj.id;
                        scrollToBottom();
                    }
                })
                .catch(function () { });
        }, 3000);
    }

    // ── Search Contacts ─────────────────────────────────
    document.getElementById('contactSearch').addEventListener('input', function () {
        var term = this.value.toLowerCase();
        document.querySelectorAll('.chat-contact').forEach(function (item) {
            item.style.display = item.dataset.ad.toLowerCase().includes(term) ? '' : 'none';
        });
    });

    // ── Helpers ─────────────────────────────────────────
    function escHtml(str) {
        if (!str) return '';
        var d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

    // ── Events ──────────────────────────────────────────
    document.getElementById('btnSend').addEventListener('click', sendMessage);
    document.getElementById('chatInput').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); sendMessage(); }
    });

    // Init
    loadContacts();

})();
