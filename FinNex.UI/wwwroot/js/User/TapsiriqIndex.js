document.querySelectorAll('.tsk-tab').forEach(btn => {
    btn.addEventListener('click', () => {
        const target = btn.dataset.tab;
        document.querySelectorAll('.tsk-tab').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        document.querySelectorAll('.tsk-panel').forEach(p => {
            p.classList.toggle('tsk-hidden', p.dataset.panel !== target);
        });
        history.replaceState(null, '', `?tab=${target}`);
    });
});