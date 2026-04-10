// ── User Chat JS (AJAX Polling) — Toplu Mesaj + Oxundu İşarələri ──

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
            html += '<div class="chat-msg ' + cls + '" data-msg-id="' + m.id + '">';
            html += '<div class="chat-msg-text">' + escHtml(m.metn) + '</div>';
            html += '<div class="chat-msg-meta">';
            html += '<span class="chat-msg-time">' + m.saatStr + '</span>';
            if (m.menimdir) {
                html += getCheckmarkHtml(m.oxunub);
            }
            html += '</div>';
            html += '</div>';
        });

        container.innerHTML = html;
        scrollToBottom();
    }

    // ── Checkmark (✓ / ✓✓) ──────────────────────────────
    function getCheckmarkHtml(oxunub) {
        if (oxunub) {
            // ✓✓ mavi — oxunub
            return '<span class="chat-check chat-check--read" title="Oxunub">✓✓</span>';
        } else {
            // ✓ boz — göndərilib
            return '<span class="chat-check chat-check--sent" title="Göndərilib">✓</span>';
        }
    }

    function appendMessage(metn, tarix, isMine, msgId) {
        var container = document.getElementById('chatMessages');
        var empty = container.querySelector('.chat-empty-state');
        if (empty) empty.remove();

        var cls = isMine ? 'chat-msg--mine' : 'chat-msg--other';
        var div = document.createElement('div');
        div.className = 'chat-msg ' + cls;
        if (msgId) div.setAttribute('data-msg-id', msgId);

        var metaHtml = '<span class="chat-msg-time">' + tarix + '</span>';
        if (isMine) {
            metaHtml += getCheckmarkHtml(false);
        }

        div.innerHTML = '<div class="chat-msg-text">' + escHtml(metn) + '</div>' +
                         '<div class="chat-msg-meta">' + metaHtml + '</div>';
        container.appendChild(div);
    }

    function scrollToBottom() {
        var c = document.getElementById('chatMessages');
        c.scrollTop = c.scrollHeight;
    }

    // ── Update read receipts for visible messages ───────
    function updateReadReceipts() {
        if (!secilmisIsciId) return;

        fetch('/User/Chat/GetReadStatus?isciId=' + secilmisIsciId)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data.oxunmuslar) return;
                data.oxunmuslar.forEach(function (msgId) {
                    var msgEl = document.querySelector('[data-msg-id="' + msgId + '"]');
                    if (msgEl) {
                        var check = msgEl.querySelector('.chat-check');
                        if (check && !check.classList.contains('chat-check--read')) {
                            check.className = 'chat-check chat-check--read';
                            check.textContent = '✓✓';
                            check.title = 'Oxunub';
                        }
                    }
                });
            })
            .catch(function () { });
    }

    // ── Send Message (AJAX POST) ────────────────────────
    function sendMessage() {
        var input = document.getElementById('chatInput');
        var metn = input.value.trim();
        if (!metn || !secilmisIsciId) return;

        var now = new Date();
        var saatStr = ('0' + now.getHours()).slice(-2) + ':' + ('0' + now.getMinutes()).slice(-2);
        appendMessage(metn, saatStr, true, null);
        scrollToBottom();
        input.value = '';

        fetch('/User/Chat/Send', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ alanIsciId: secilmisIsciId, metn: metn })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.ok && data.id) {
                // Son əlavə edilmiş mesaja ID əlavə et
                var allMsgs = document.querySelectorAll('.chat-msg--mine');
                var lastMsg = allMsgs[allMsgs.length - 1];
                if (lastMsg && !lastMsg.getAttribute('data-msg-id')) {
                    lastMsg.setAttribute('data-msg-id', data.id);
                }
            }
        })
        .catch(function (err) {
            console.error('Mesaj göndərmə xətası:', err);
        });
    }

    // ── Polling: hər 3 saniyədə yeni mesaj yoxla ────────
    function startPolling() {
        if (pollTimer) clearInterval(pollTimer);
        pollTimer = setInterval(function () {
            if (!secilmisIsciId) return;

            // Yeni mesajları yoxla
            fetch('/User/Chat/GetMessages?isciId=' + secilmisIsciId)
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (!data.mesajlar || data.mesajlar.length === 0) return;
                    var sonMesaj = data.mesajlar[data.mesajlar.length - 1];
                    if (sonMesaj.id > lastMesajId) {
                        var yeniMesajlar = data.mesajlar.filter(function (m) { return m.id > lastMesajId; });
                        yeniMesajlar.forEach(function (m) {
                            if (!m.menimdir) {
                                appendMessage(m.metn, m.saatStr, false, m.id);
                            }
                        });
                        lastMesajId = sonMesaj.id;
                        scrollToBottom();
                    }
                })
                .catch(function () { });

            // Oxundu statuslarını yenilə
            updateReadReceipts();
        }, 3000);
    }

    // ── Search Contacts ─────────────────────────────────
    document.getElementById('contactSearch').addEventListener('input', function () {
        var term = this.value.toLowerCase();
        document.querySelectorAll('.chat-contact').forEach(function (item) {
            item.style.display = item.dataset.ad.toLowerCase().includes(term) ? '' : 'none';
        });
    });

    // ══════════════════════════════════════════════════════
    // ── Toplu Mesaj (Bulk Messaging) ─────────────────────
    // ══════════════════════════════════════════════════════

    function openBulkModal() {
        var modal = document.getElementById('bulkModal');
        modal.style.display = 'flex';
        document.getElementById('bulkMetn').value = '';
        document.getElementById('bulkInfo').textContent = '';
        loadDepartments();
    }

    function closeBulkModal() {
        document.getElementById('bulkModal').style.display = 'none';
    }

    function loadDepartments() {
        var select = document.getElementById('bulkTarget');
        // Mövcud option-ları sil (ilk "Bütün işçilər" saxla)
        while (select.options.length > 1) {
            select.remove(1);
        }

        fetch('/User/Chat/GetDepartments')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.departamentler) {
                    data.departamentler.forEach(function (d) {
                        var opt = document.createElement('option');
                        opt.value = d.id;
                        opt.textContent = d.ad;
                        select.appendChild(opt);
                    });
                }
            })
            .catch(function () {
                console.error('Departament siyahısı yüklənmədi');
            });
    }

    function sendBulkMessage() {
        var metn = document.getElementById('bulkMetn').value.trim();
        if (!metn) {
            document.getElementById('bulkInfo').textContent = 'Mesaj mətni boş ola bilməz!';
            document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--error';
            return;
        }

        var targetVal = document.getElementById('bulkTarget').value;
        var departamentId = targetVal === 'all' ? null : parseInt(targetVal);

        var btn = document.getElementById('btnSendBulk');
        btn.disabled = true;
        btn.textContent = 'Göndərilir...';

        fetch('/User/Chat/SendBulk', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ departamentId: departamentId, metn: metn })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            btn.disabled = false;
            btn.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:4px"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg> Göndər';

            if (data.ok) {
                document.getElementById('bulkInfo').textContent =
                    data.say + ' işçiyə mesaj göndərildi!';
                document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--success';
                document.getElementById('bulkMetn').value = '';

                // 2 saniyə sonra modal bağla və kontaktları yenilə
                setTimeout(function () {
                    closeBulkModal();
                    loadContacts();
                }, 1500);
            } else {
                document.getElementById('bulkInfo').textContent = data.mesaj || 'Xəta baş verdi';
                document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--error';
            }
        })
        .catch(function () {
            btn.disabled = false;
            btn.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:4px"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg> Göndər';
            document.getElementById('bulkInfo').textContent = 'Şəbəkə xətası!';
            document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--error';
        });
    }

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

    // Toplu mesaj modal eventləri
    document.getElementById('btnTopluMesaj').addEventListener('click', openBulkModal);
    document.getElementById('btnCloseBulk').addEventListener('click', closeBulkModal);
    document.getElementById('btnCancelBulk').addEventListener('click', closeBulkModal);
    document.getElementById('btnSendBulk').addEventListener('click', sendBulkMessage);

    // Modal overlay kliklə bağla
    document.getElementById('bulkModal').addEventListener('click', function (e) {
        if (e.target === this) closeBulkModal();
    });

    // Init
    loadContacts();

})();
