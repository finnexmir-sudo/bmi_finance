// ── User Chat JS — Pagination + Emoji + Fayl + Read Receipts ──

// Global: mesaj silmə
function chatDeleteMsg(msgId) {
    if (!confirm('Bu mesajı silmək istəyirsiniz?')) return;
    fetch('/User/Chat/DeleteMessage', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mesajId: msgId })
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        if (data.ok) {
            var el = document.querySelector('[data-msg-id="' + msgId + '"]');
            if (el) el.remove();
        }
    })
    .catch(function () { });
}

(function () {
    'use strict';

    var menimIsciId = 0;
    var secilmisIsciId = 0;
    var pollTimer = null;
    var lastMesajId = 0;
    var enKicikMesajId = 0;
    var dahaKohneMesajVar = false;
    var yuklenir = false;
    var secilmisFayl = null;

    // ── Emoji siyahısı ──────────────────────────────────
    var emojiList = [
        '😀','😁','😂','🤣','😃','😄','😅','😆','😉','😊',
        '😋','😎','🥳','😍','🥰','😘','😗','😙','😚','🤗',
        '🤔','🤨','😐','😑','😶','🙄','😏','😣','😥','😮',
        '🤐','😯','😪','😫','🥱','😴','😌','😛','😜','😝',
        '🤤','😒','😓','😔','😕','🙃','🤑','😲','🙁','😖',
        '😞','😟','😤','😢','😭','😦','😧','😨','😩','🤯',
        '😬','😰','😱','🥵','🥶','😳','🤪','😵','🥴','😠',
        '😡','🤬','👍','👎','👏','🙏','🤝','💪','❤️','🔥',
        '⭐','✅','❌','⚠️','💯','🎉','🎊','📎','📁','📄'
    ];

    // ══════════════════════════════════════════════════════
    // ── Contacts ─────────────────────────────────────────
    // ══════════════════════════════════════════════════════

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
            html += '<div class="chat-contact-info"><div class="chat-contact-name">' + escHtml(c.ad + ' ' + c.soyad) + '</div></div>';
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

    function selectContact(isciId, ad, el) {
        secilmisIsciId = isciId;
        document.getElementById('secilmisIsciId').value = isciId;
        document.querySelectorAll('.chat-contact').forEach(function (c) { c.classList.remove('chat-contact--active'); });
        if (el) el.classList.add('chat-contact--active');
        document.getElementById('chatHeader').style.display = 'flex';
        document.getElementById('chatName').textContent = ad;
        var initials = ad.split(' ').map(function (w) { return w[0] || ''; }).join('').substring(0, 2);
        document.getElementById('chatAvatar').textContent = initials.toUpperCase();
        document.getElementById('chatInputArea').style.display = 'flex';
        var badge = document.querySelector('[data-unread-id="' + isciId + '"]');
        if (badge) badge.remove();
        clearFile();
        lastMesajId = 0;
        enKicikMesajId = 0;
        dahaKohneMesajVar = false;
        loadMessages(isciId, 0);
        startPolling();
    }

    // ══════════════════════════════════════════════════════
    // ── Messages (Pagination) ────────────────────────────
    // ══════════════════════════════════════════════════════

    function loadMessages(isciId, beforeId) {
        var container = document.getElementById('chatMessages');
        if (beforeId === 0) {
            container.innerHTML = '<div class="chat-loading" style="color:#94a3b8">Yüklənir...</div>';
        }
        yuklenir = true;

        var url = '/User/Chat/GetMessages?isciId=' + isciId;
        if (beforeId > 0) url += '&beforeId=' + beforeId;

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                yuklenir = false;
                if (beforeId === 0) {
                    renderMessages(data.mesajlar);
                } else {
                    prependMessages(data.mesajlar);
                }
                dahaKohneMesajVar = data.dahaVar;

                if (data.mesajlar && data.mesajlar.length > 0) {
                    if (beforeId === 0) {
                        lastMesajId = data.mesajlar[data.mesajlar.length - 1].id;
                    }
                    var ilkId = data.mesajlar[0].id;
                    if (enKicikMesajId === 0 || ilkId < enKicikMesajId) {
                        enKicikMesajId = ilkId;
                    }
                }
            })
            .catch(function () {
                yuklenir = false;
                if (beforeId === 0) {
                    container.innerHTML = '<div class="chat-loading" style="color:#dc2626">Xəta baş verdi</div>';
                }
            });
    }

    function renderMessages(mesajlar) {
        var container = document.getElementById('chatMessages');
        if (!mesajlar || mesajlar.length === 0) {
            container.innerHTML = '<div class="chat-empty-state"><div class="chat-empty-title">Hələ mesaj yoxdur</div><div class="chat-empty-sub">İlk mesajı göndərin</div></div>';
            return;
        }
        var html = '';
        if (dahaKohneMesajVar) {
            html += '<div class="chat-load-more" id="loadMoreBtn">↑ Köhnə mesajları yüklə</div>';
        }
        var lastDate = '';
        mesajlar.forEach(function (m) {
            html += buildMsgHtml(m, lastDate);
            lastDate = m.tarix.split(' ')[0];
        });
        container.innerHTML = html;
        scrollToBottom();
        bindLoadMore();
    }

    function prependMessages(mesajlar) {
        if (!mesajlar || mesajlar.length === 0) return;
        var container = document.getElementById('chatMessages');
        var oldScrollH = container.scrollHeight;

        var oldLoadMore = document.getElementById('loadMoreBtn');
        if (oldLoadMore) oldLoadMore.remove();

        var html = '';
        if (dahaKohneMesajVar) {
            html += '<div class="chat-load-more" id="loadMoreBtn">↑ Köhnə mesajları yüklə</div>';
        }
        var lastDate = '';
        mesajlar.forEach(function (m) {
            html += buildMsgHtml(m, lastDate);
            lastDate = m.tarix.split(' ')[0];
        });

        container.insertAdjacentHTML('afterbegin', html);
        container.scrollTop = container.scrollHeight - oldScrollH;
        bindLoadMore();
    }

    function buildMsgHtml(m, prevDate) {
        var html = '';
        var msgDate = m.tarix.split(' ')[0];
        if (msgDate !== prevDate) {
            html += '<div class="chat-date-sep">' + msgDate + '</div>';
        }
        var cls = m.menimdir ? 'chat-msg--mine' : 'chat-msg--other';
        html += '<div class="chat-msg ' + cls + '" data-msg-id="' + m.id + '">';
        if (m.menimdir) html += '<button class="chat-msg-delete" onclick="chatDeleteMsg(' + m.id + ')" title="Sil">&times;</button>';
        if (m.metn) html += '<div class="chat-msg-text">' + escHtml(m.metn) + '</div>';
        if (m.faylAdi) html += buildFileHtml(m.faylAdi, m.faylYolu, m.faylTipi, m.faylOlcusu);
        html += '<div class="chat-msg-meta"><span class="chat-msg-time">' + m.saatStr + '</span>';
        if (m.menimdir) html += getCheckmarkHtml(m.oxunub);
        html += '</div></div>';
        return html;
    }

    function buildFileHtml(faylAdi, faylYolu, faylTipi, faylOlcusu) {
        var icon = '📄';
        if (faylTipi === 'pdf') icon = '📕';
        else if (faylTipi === 'doc' || faylTipi === 'docx') icon = '📘';
        else if (faylTipi === 'xls' || faylTipi === 'xlsx') icon = '📗';

        var size = faylOlcusu ? formatFileSize(faylOlcusu) : '';
        return '<a href="' + faylYolu + '" target="_blank" class="chat-file-attach">' +
               '<span class="chat-file-icon">' + icon + '</span>' +
               '<span class="chat-file-name">' + escHtml(faylAdi) + '</span>' +
               '<span class="chat-file-size">' + size + '</span></a>';
    }

    function formatFileSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / 1048576).toFixed(1) + ' MB';
    }

    function bindLoadMore() {
        var btn = document.getElementById('loadMoreBtn');
        if (btn) {
            btn.addEventListener('click', function () {
                if (!yuklenir && enKicikMesajId > 0) {
                    loadMessages(secilmisIsciId, enKicikMesajId);
                }
            });
        }
    }

    // ── Scroll ilə köhnə mesaj yükləmə ─────────────────
    document.getElementById('chatMessages').addEventListener('scroll', function () {
        if (this.scrollTop < 50 && dahaKohneMesajVar && !yuklenir && enKicikMesajId > 0) {
            loadMessages(secilmisIsciId, enKicikMesajId);
        }
    });

    // ══════════════════════════════════════════════════════
    // ── Checkmarks ───────────────────────────────────────
    // ══════════════════════════════════════════════════════

    function getCheckmarkHtml(oxunub) {
        if (oxunub) return '<span class="chat-check chat-check--read" title="Oxunub">✓✓</span>';
        return '<span class="chat-check chat-check--sent" title="Göndərilib">✓</span>';
    }

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
            }).catch(function () { });
    }

    // ══════════════════════════════════════════════════════
    // ── Send Message ─────────────────────────────────────
    // ══════════════════════════════════════════════════════

    function sendMessage() {
        var input = document.getElementById('chatInput');
        var metn = input.value.trim();
        if (!secilmisIsciId) return;
        if (!metn && !secilmisFayl) return;

        var now = new Date();
        var saatStr = ('0' + now.getHours()).slice(-2) + ':' + ('0' + now.getMinutes()).slice(-2);

        if (secilmisFayl) {
            // FormData ilə fayl göndər
            var fd = new FormData();
            fd.append('alanIsciId', secilmisIsciId);
            fd.append('metn', metn);
            fd.append('fayl', secilmisFayl);

            appendMessage(metn, saatStr, true, null, secilmisFayl.name, null, null, secilmisFayl.size);
            scrollToBottom();
            input.value = '';
            clearFile();

            fetch('/User/Chat/SendWithFile', { method: 'POST', body: fd })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (data.ok && data.id) {
                        var allMsgs = document.querySelectorAll('.chat-msg--mine');
                        var lastMsg = allMsgs[allMsgs.length - 1];
                        if (lastMsg && !lastMsg.getAttribute('data-msg-id')) {
                            lastMsg.setAttribute('data-msg-id', data.id);
                            lastMsg.insertAdjacentHTML('afterbegin', '<button class="chat-msg-delete" onclick="chatDeleteMsg(' + data.id + ')" title="Sil">&times;</button>');
                        }
                    }
                }).catch(function (err) { console.error('Fayl göndərmə xətası:', err); });
        } else {
            appendMessage(metn, saatStr, true, null, null, null, null, null);
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
                    var allMsgs = document.querySelectorAll('.chat-msg--mine');
                    var lastMsg = allMsgs[allMsgs.length - 1];
                    if (lastMsg && !lastMsg.getAttribute('data-msg-id')) {
                        lastMsg.setAttribute('data-msg-id', data.id);
                        lastMsg.insertAdjacentHTML('afterbegin', '<button class="chat-msg-delete" onclick="chatDeleteMsg(' + data.id + ')" title="Sil">&times;</button>');
                    }
                }
            }).catch(function (err) { console.error('Mesaj göndərmə xətası:', err); });
        }
    }

    function appendMessage(metn, tarix, isMine, msgId, faylAdi, faylYolu, faylTipi, faylOlcusu) {
        var container = document.getElementById('chatMessages');
        var empty = container.querySelector('.chat-empty-state');
        if (empty) empty.remove();

        // Tarix ayırıcısı — bu gün üçün separator yoxdursa əlavə et
        var now = new Date();
        var todayStr = ('0' + now.getDate()).slice(-2) + '.' +
                       ('0' + (now.getMonth() + 1)).slice(-2) + '.' +
                       now.getFullYear();
        var seps = container.querySelectorAll('.chat-date-sep');
        var lastSepText = seps.length > 0 ? seps[seps.length - 1].textContent.trim() : '';
        if (lastSepText !== todayStr) {
            var sep = document.createElement('div');
            sep.className = 'chat-date-sep';
            sep.textContent = todayStr;
            container.appendChild(sep);
        }

        var cls = isMine ? 'chat-msg--mine' : 'chat-msg--other';
        var div = document.createElement('div');
        div.className = 'chat-msg ' + cls;
        if (msgId) div.setAttribute('data-msg-id', msgId);

        var inner = '';
        if (isMine && msgId) inner += '<button class="chat-msg-delete" onclick="chatDeleteMsg(' + msgId + ')" title="Sil">&times;</button>';
        if (metn) inner += '<div class="chat-msg-text">' + escHtml(metn) + '</div>';
        if (faylAdi) {
            if (faylYolu) {
                inner += buildFileHtml(faylAdi, faylYolu, faylTipi, faylOlcusu);
            } else {
                inner += '<div class="chat-file-attach"><span class="chat-file-icon">📎</span><span class="chat-file-name">' + escHtml(faylAdi) + '</span><span class="chat-file-size">' + formatFileSize(faylOlcusu || 0) + '</span></div>';
            }
        }
        var metaHtml = '<span class="chat-msg-time">' + tarix + '</span>';
        if (isMine) metaHtml += getCheckmarkHtml(false);
        inner += '<div class="chat-msg-meta">' + metaHtml + '</div>';

        div.innerHTML = inner;
        container.appendChild(div);
    }

    function scrollToBottom() {
        var c = document.getElementById('chatMessages');
        c.scrollTop = c.scrollHeight;
    }

    // ══════════════════════════════════════════════════════
    // ── Emoji Picker ─────────────────────────────────────
    // ══════════════════════════════════════════════════════

    function initEmojiPanel() {
        var panel = document.getElementById('emojiPanel');
        var html = '';
        emojiList.forEach(function (e) {
            html += '<span class="chat-emoji-item">' + e + '</span>';
        });
        panel.innerHTML = html;

        panel.querySelectorAll('.chat-emoji-item').forEach(function (item) {
            item.addEventListener('click', function () {
                var input = document.getElementById('chatInput');
                input.value += this.textContent;
                input.focus();
                panel.style.display = 'none';
            });
        });
    }

    document.getElementById('btnEmoji').addEventListener('click', function (e) {
        e.stopPropagation();
        var panel = document.getElementById('emojiPanel');
        panel.style.display = panel.style.display === 'none' ? 'grid' : 'none';
    });

    document.addEventListener('click', function (e) {
        var panel = document.getElementById('emojiPanel');
        if (panel.style.display !== 'none' && !e.target.closest('.chat-emoji-wrap')) {
            panel.style.display = 'none';
        }
    });

    // ══════════════════════════════════════════════════════
    // ── File Attach ──────────────────────────────────────
    // ══════════════════════════════════════════════════════

    document.getElementById('btnAttach').addEventListener('click', function () {
        document.getElementById('fileInput').click();
    });

    document.getElementById('fileInput').addEventListener('change', function () {
        var file = this.files[0];
        if (!file) return;

        if (file.size > 10 * 1024 * 1024) {
            alert('Fayl ölçüsü 10MB-dan çox ola bilməz');
            this.value = '';
            return;
        }

        secilmisFayl = file;
        document.getElementById('filePreviewName').textContent = file.name;
        document.getElementById('filePreviewSize').textContent = formatFileSize(file.size);
        document.getElementById('filePreview').style.display = 'flex';
    });

    document.getElementById('btnClearFile').addEventListener('click', clearFile);

    function clearFile() {
        secilmisFayl = null;
        document.getElementById('fileInput').value = '';
        document.getElementById('filePreview').style.display = 'none';
    }

    // ══════════════════════════════════════════════════════
    // ── Polling ──────────────────────────────────────────
    // ══════════════════════════════════════════════════════

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
                        var yeniMesajlar = data.mesajlar.filter(function (m) { return m.id > lastMesajId; });
                        yeniMesajlar.forEach(function (m) {
                            if (!m.menimdir) {
                                appendMessage(m.metn, m.saatStr, false, m.id, m.faylAdi, m.faylYolu, m.faylTipi, m.faylOlcusu);
                            }
                        });
                        lastMesajId = sonMesaj.id;
                        scrollToBottom();
                    }
                }).catch(function () { });
            updateReadReceipts();
        }, 5000);
    }

    // ══════════════════════════════════════════════════════
    // ── Search + Toplu Mesaj ─────────────────────────────
    // ══════════════════════════════════════════════════════

    document.getElementById('contactSearch').addEventListener('input', function () {
        var term = this.value.toLowerCase();
        document.querySelectorAll('.chat-contact').forEach(function (item) {
            item.style.display = item.dataset.ad.toLowerCase().includes(term) ? '' : 'none';
        });
    });

    // ── Toplu mesaj funksiyaları ─────────────────────────
    function openBulkModal() {
        document.getElementById('bulkModal').style.display = 'flex';
        document.getElementById('bulkMetn').value = '';
        document.getElementById('bulkInfo').textContent = '';
        document.getElementById('bulkSearch').value = '';
        loadDepartments();
        loadEmployees(null);
    }
    function closeBulkModal() { document.getElementById('bulkModal').style.display = 'none'; }

    function loadDepartments() {
        var select = document.getElementById('bulkTarget');
        while (select.options.length > 1) select.remove(1);
        fetch('/User/Chat/GetDepartments').then(function (r) { return r.json(); }).then(function (data) {
            if (data.departamentler) {
                data.departamentler.forEach(function (d) {
                    var opt = document.createElement('option');
                    opt.value = d.id; opt.textContent = d.ad;
                    select.appendChild(opt);
                });
            }
        }).catch(function () { });
    }

    function loadEmployees(departamentId) {
        var list = document.getElementById('bulkEmployeeList');
        list.innerHTML = '<div class="chat-loading">Yüklənir...</div>';
        var url = '/User/Chat/GetEmployees';
        if (departamentId && departamentId !== 'all') url += '?departamentId=' + departamentId;
        fetch(url).then(function (r) { return r.json(); }).then(function (data) {
            renderEmployeeCheckboxes(data.isciler);
        }).catch(function () { list.innerHTML = '<div class="chat-loading">Xəta</div>'; });
    }

    function renderEmployeeCheckboxes(isciler) {
        var list = document.getElementById('bulkEmployeeList');
        if (!isciler || isciler.length === 0) { list.innerHTML = '<div class="chat-loading">İşçi tapılmadı</div>'; updateBulkCount(); return; }
        var html = '';
        isciler.forEach(function (i) {
            html += '<label class="bulk-emp-item" data-name="' + escHtml(i.ad + ' ' + i.soyad).toLowerCase() + '">';
            html += '<input type="checkbox" class="bulk-emp-cb" value="' + i.id + '" checked />';
            html += '<span class="bulk-emp-name">' + escHtml(i.ad + ' ' + i.soyad) + '</span>';
            if (i.departament) html += '<span class="bulk-emp-dept">(' + escHtml(i.departament) + ')</span>';
            html += '</label>';
        });
        list.innerHTML = html;
        list.querySelectorAll('.bulk-emp-cb').forEach(function (cb) { cb.addEventListener('change', updateBulkCount); });
        updateBulkCount();
    }

    function updateBulkCount() {
        var checked = document.querySelectorAll('.bulk-emp-cb:checked').length;
        var total = document.querySelectorAll('.bulk-emp-cb').length;
        document.getElementById('bulkCount').textContent = checked + ' / ' + total + ' seçili';
    }

    function sendBulkMessage() {
        var metn = document.getElementById('bulkMetn').value.trim();
        if (!metn) { document.getElementById('bulkInfo').textContent = 'Mesaj mətni boş ola bilməz!'; document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--error'; return; }
        var ids = []; document.querySelectorAll('.bulk-emp-cb:checked').forEach(function (cb) { ids.push(parseInt(cb.value)); });
        if (ids.length === 0) { document.getElementById('bulkInfo').textContent = 'Ən azı bir işçi seçilməlidir!'; document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--error'; return; }
        var btn = document.getElementById('btnSendBulk'); btn.disabled = true; btn.textContent = 'Göndərilir...';
        fetch('/User/Chat/SendBulk', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ isciIdler: ids, metn: metn }) })
        .then(function (r) { return r.json(); }).then(function (data) {
            btn.disabled = false; btn.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:4px"><line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/></svg> Göndər';
            if (data.ok) { document.getElementById('bulkInfo').textContent = data.say + ' işçiyə mesaj göndərildi!'; document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--success'; document.getElementById('bulkMetn').value = ''; setTimeout(function () { closeBulkModal(); loadContacts(); }, 1500); }
            else { document.getElementById('bulkInfo').textContent = data.mesaj || 'Xəta'; document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--error'; }
        }).catch(function () { btn.disabled = false; btn.innerHTML = 'Göndər'; document.getElementById('bulkInfo').textContent = 'Şəbəkə xətası!'; document.getElementById('bulkInfo').className = 'chat-bulk-info chat-bulk-info--error'; });
    }

    // ── Helpers ──────────────────────────────────────────
    function escHtml(str) {
        if (!str) return '';
        var d = document.createElement('div'); d.textContent = str; return d.innerHTML;
    }

    // ══════════════════════════════════════════════════════
    // ── Events ───────────────────────────────────────────
    // ══════════════════════════════════════════════════════

    document.getElementById('btnSend').addEventListener('click', sendMessage);

    // Enter = göndər, Shift+Enter = yeni sətir
    document.getElementById('chatInput').addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); }
    });

    // Textarea avtomatik böyüsün
    document.getElementById('chatInput').addEventListener('input', function () {
        this.style.height = 'auto';
        this.style.height = Math.min(this.scrollHeight, 100) + 'px';
    });

    document.getElementById('btnTopluMesaj').addEventListener('click', openBulkModal);
    document.getElementById('btnCloseBulk').addEventListener('click', closeBulkModal);
    document.getElementById('btnCancelBulk').addEventListener('click', closeBulkModal);
    document.getElementById('btnSendBulk').addEventListener('click', sendBulkMessage);
    document.getElementById('bulkModal').addEventListener('click', function (e) { if (e.target === this) closeBulkModal(); });
    document.getElementById('bulkTarget').addEventListener('change', function () { loadEmployees(this.value === 'all' ? null : this.value); });
    document.getElementById('btnSelectAll').addEventListener('click', function () { document.querySelectorAll('.bulk-emp-cb').forEach(function (cb) { if (cb.closest('.bulk-emp-item').style.display !== 'none') cb.checked = true; }); updateBulkCount(); });
    document.getElementById('btnDeselectAll').addEventListener('click', function () { document.querySelectorAll('.bulk-emp-cb').forEach(function (cb) { cb.checked = false; }); updateBulkCount(); });
    document.getElementById('bulkSearch').addEventListener('input', function () { var term = this.value.toLowerCase(); document.querySelectorAll('.bulk-emp-item').forEach(function (item) { item.style.display = item.dataset.name.includes(term) ? '' : 'none'; }); });

    // Init
    initEmojiPanel();
    loadContacts();

})();
