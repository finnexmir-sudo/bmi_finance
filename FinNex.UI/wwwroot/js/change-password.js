/**
 * FinNex — change-password.js
 */
(function () {
    'use strict';

    var newInput = document.getElementById('inp-new');
    var confirmInput = document.getElementById('inp-confirm');
    var strengthFill = document.getElementById('strengthFill');
    var strengthLabel = document.getElementById('strengthLabel');
    var matchMsg = document.getElementById('matchMsg');
    var cpBtn = document.getElementById('cpBtn');
    var cpBtnText = document.getElementById('cpBtnText');
    var cpBtnSpinner = document.getElementById('cpBtnSpinner');

    /* ── Göz düyməsi ── */
    document.querySelectorAll('.cp-eye').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var inp = document.getElementById(btn.dataset.target);
            if (!inp) return;
            var show = inp.type === 'password';
            inp.type = show ? 'text' : 'password';
            var icon = btn.querySelector('i');
            if (icon) icon.className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
        });
    });

    /* ── Qaydalar ── */
    var rules = {
        'rule-length': function (v) { return v.length >= 8; },
        'rule-upper': function (v) { return /[A-Z]/.test(v); },
        'rule-number': function (v) { return /[0-9]/.test(v); },
        'rule-special': function (v) { return /[^A-Za-z0-9]/.test(v); }
    };

    var levels = [
        { lbl: '', clr: 'transparent', w: 0 },
        { lbl: 'Zeif', clr: '#ef4444', w: 25 },
        { lbl: 'Orta', clr: '#f59e0b', w: 50 },
        { lbl: 'Yaxsi', clr: '#3b82f6', w: 75 },
        { lbl: 'Guclu', clr: '#10b981', w: 100 }
    ];

    function evalRules(val) {
        var score = 0;
        Object.keys(rules).forEach(function (id) {
            var el = document.getElementById(id);
            var ok = rules[id](val);
            if (ok) score++;
            if (!el) return;
            el.classList.toggle('passed', ok);
            var sp = el.querySelector('span');
            if (sp) sp.textContent = ok ? '\u2713' : '\u25cb';
        });
        return score;
    }

    function refreshStrength(val) {
        var s = val ? evalRules(val) : 0;
        var l = levels[s];
        if (strengthFill) { strengthFill.style.width = l.w + '%'; strengthFill.style.background = l.clr; }
        if (strengthLabel) { strengthLabel.textContent = l.lbl; strengthLabel.style.color = l.clr; }
    }

    function refreshMatch() {
        if (!matchMsg) return;
        var nv = newInput ? newInput.value : '';
        var cv = confirmInput ? confirmInput.value : '';
        if (!cv) { matchMsg.textContent = ''; matchMsg.className = 'cp-match'; return; }
        if (nv === cv) {
            matchMsg.textContent = '\u2713 Sifreler uygundur';
            matchMsg.className = 'cp-match ok';
        } else {
            matchMsg.textContent = '\u2717 Sifreler uygun deyil';
            matchMsg.className = 'cp-match bad';
        }
    }

    if (newInput) newInput.addEventListener('input', function () { refreshStrength(newInput.value); refreshMatch(); });
    if (confirmInput) confirmInput.addEventListener('input', refreshMatch);

    /* ── Spinner ── */
    var form = document.getElementById('cpForm');
    if (form) {
        form.addEventListener('submit', function () {
            if (cpBtn) cpBtn.disabled = true;
            if (cpBtnText) cpBtnText.classList.add('d-none');
            if (cpBtnSpinner) cpBtnSpinner.classList.remove('d-none');
        });
    }
}());