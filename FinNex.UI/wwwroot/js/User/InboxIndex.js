document.querySelectorAll('.ibx-tab').forEach(btn => {
    btn.addEventListener('click', () => {
        const target = btn.dataset.tab;
        document.querySelectorAll('.ibx-tab').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        document.querySelectorAll('.ibx-panel').forEach(p => {
            p.classList.toggle('ibx-hidden', p.dataset.panel !== target);
        });
        history.replaceState(null, '', `?tab=${target}`);
    });
});

// Bildiriş oxundu
document.querySelectorAll('.ibx-item[data-bildiris-id]').forEach(item => {
    item.addEventListener('click', async () => {
        const id = item.dataset.bildirisId;
        if (item.classList.contains('ibx-item--unread')) {
            await fetch('/User/Inbox/BildirisOxu', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? ''
                },
                body: JSON.stringify(parseInt(id))
            });
            item.classList.remove('ibx-item--unread');
            item.querySelector('.ibx-unread-dot')?.remove();
        }
    });
});

// Hamısını oxu
document.getElementById('hamisiniOxuBtn')?.addEventListener('click', async () => {
    await fetch('/User/Inbox/HamisiniOxu', { method: 'POST' });
    document.querySelectorAll('.ibx-item--unread').forEach(i => {
        i.classList.remove('ibx-item--unread');
        i.querySelector('.ibx-unread-dot')?.remove();
    });
});

// Rədd modal
function openReddModal(id) {
    document.getElementById('reddSorguId').value = id;
    document.getElementById('reddModal').classList.add('open');
}

function closeReddModal() {
    document.getElementById('reddModal').classList.remove('open');
}

document.getElementById('reddModal')?.addEventListener('click', e => {
    if (e.target === document.getElementById('reddModal')) closeReddModal();
});