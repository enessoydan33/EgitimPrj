// Sınıf ve öğrenci listeleri, API'den gelen verilere göre doldurulacak.
let teacherClassrooms = [];
let classroomStudentsCache = new Map(); // classroomId -> öğrenci listesi

let sentItems = [];

const subjectColors = {
    matematik:"#6366f1", fizik:"#3b82f6", kimya:"#10b981",
    biyoloji:"#22c55e", turkce:"#f59e0b", tarih:"#ef4444",
    cografya:"#8b5cf6"
};

// Hedef butonları
function setupTargetBtns(formId, classRowId, studentRowId) {
    const form       = document.getElementById(formId);
    const classRow   = document.getElementById(classRowId);
    const studentRow = document.getElementById(studentRowId);
    form.querySelectorAll('.target-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            form.querySelectorAll('.target-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const isClass = btn.dataset.target === 'class';
            classRow.classList.toggle('d-none', !isClass);
            studentRow.classList.toggle('d-none', isClass);
        });
    });
}
setupTargetBtns('homework-form', 'hw-class-row', 'hw-student-row');

// API yardımcıları
async function apiGet(url) {
    const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
}

async function apiPost(url, body) {
    const res = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        },
        body: JSON.stringify(body ?? {})
    });
    if (!res.ok) {
        const errText = await res.text();
        console.error("API / Proxy 500 Hata Detayı:", errText);
        throw new Error(`HTTP ${res.status}`);
    }
    return await res.json();
}

function classroomLabel(c) {
    // ClassroomDto: { id, name, description, studentCount }
    return c?.name || `Sınıf #${c?.id}`;
}

// Sınıf dropdown'unu doldur
async function loadClassrooms() {
    try {
        const data = await apiGet('/TeacherPanelProxy/Classrooms');
        teacherClassrooms = Array.isArray(data) ? data : [];

        const select = document.getElementById('hw-class');
        if (!select) return;

        select.innerHTML = '';
        teacherClassrooms.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = classroomLabel(c);
            select.appendChild(opt);
        });
    } catch (err) {
        console.error('Sınıflar yüklenemedi', err);
    }
}

// Basit örnek: sınıfın öğrencilerini performans API'sinden çekiyoruz
async function loadStudentsForClassroom(classroomId) {
    if (!classroomId) return [];
    if (classroomStudentsCache.has(classroomId)) {
        return classroomStudentsCache.get(classroomId);
    }

    try {
        const performances = await apiGet(`/TeacherPanelProxy/ClassroomPerformances?classroomId=${encodeURIComponent(classroomId)}`);
        // StudentPerformanceDto: { studentId, studentName, avatarUrl, ... }
        const students = (performances || []).map(p => ({
            id: p.studentId,
            name: p.studentName,
            cls: classroomLabel(teacherClassrooms.find(c => c.id === classroomId)) || ''
        }));
        classroomStudentsCache.set(classroomId, students);
        return students;
    } catch (err) {
        console.error('Öğrenciler yüklenemedi', err);
        return [];
    }
}

// Öğrenci dropdown'unu güncelle
async function refreshStudentDropdown() {
    const classSelect = document.getElementById('hw-class');
    const studentSelect = document.getElementById('hw-student');
    if (!classSelect || !studentSelect) return;

    const classroomId = parseInt(classSelect.value);
    const students = await loadStudentsForClassroom(classroomId);

    studentSelect.innerHTML = '<option value=\"\">Öğrenci seçin...</option>';
    students.forEach(s => {
        const opt = document.createElement('option');
        opt.value = s.id;
        opt.textContent = `${s.name}`;
        studentSelect.appendChild(opt);
    });
}

document.getElementById('hw-class')?.addEventListener('change', () => {
    refreshStudentDropdown();
});

// Gönderilen ödevler tablosu
function renderSentList() {
    const tbody = document.getElementById('sent-table-body');
    const empty = document.getElementById('sent-empty');
    const table = document.getElementById('sent-table');
    document.getElementById('sent-count').textContent = sentItems.length + ' Kayıt';

    if (!sentItems.length) {
        tbody.innerHTML = '';
        table.classList.add('d-none');
        empty.classList.remove('d-none');
        return;
    }
    table.classList.remove('d-none');
    empty.classList.add('d-none');

    tbody.innerHTML = sentItems.map(item => {
        const color = subjectColors[item.subject?.toLowerCase?.() || item.subject] || '#6366f1';
        const toText = item.classroomName
            ? `<i class="bi bi-people me-1"></i>${item.classroomName}`
            : `<i class="bi bi-person me-1"></i>${item.userName || 'Öğrenci'}`;
        const doneCount = (item.completedBy || []).length;
        // The total count can be async so we'll just show doneCount or wait to calculate it
        const checkLabel = doneCount > 0 ? `Kontrol Edildi` : 'Kontrol';
        return `
        <tr>
            <td style="border-left-color:${color}">
                <button class="btn-check-hw" onclick="openCheckModal(${item.id})">
                    <i class="bi bi-clipboard-check me-1"></i>${checkLabel}
                    ${doneCount > 0 ? `<span class="check-count">${doneCount}</span>` : ''}
                </button>
            </td>
            <td>
                <span class="fw-semibold">${toText}</span>
            </td>
            <td><span class="subject-badge" style="background:${color}15;color:${color}">${item.subject}</span></td>
            <td class="fw-bold">${item.topic}</td>
            <td><i class="bi bi-calendar3 me-1 text-muted"></i>${formatDate(item.dueDate)}</td>
            <td class="text-muted">${item.description ? item.description.substring(0,50) + (item.description.length > 50 ? '…' : '') : '–'}</td>
            <td></td>
        </tr>`;
    }).join('');
}

// Form submit
document.getElementById('homework-form').addEventListener('submit', async e => {
    e.preventDefault();
    const isClass    = document.querySelector('#homework-form .target-btn.active').dataset.target === 'class';
    const classSelect = document.getElementById('hw-class');
    const studentSel = document.getElementById('hw-student');
    const classroomId = isClass ? parseInt(classSelect.value) || null : null;
    const studentId  = isClass ? null : (parseInt(studentSel.value) || null);
    const subject    = document.getElementById('hw-subject').value;
    const title      = document.getElementById('hw-title').value.trim();
    const due        = document.getElementById('hw-due').value;
    const desc       = document.getElementById('hw-desc').value.trim();

    if (!title || !due || (!classroomId && !studentId)) return;

    const payload = {
        subject: subject,
        topic: title,
        description: desc || null,
        dueDate: due,
        classroomId: classroomId,
        userId: studentId
    };

    const el = document.getElementById('hw-feedback');

    try {
        const created = await apiPost('/TeacherPanelProxy/CreateHomework', payload);
        // API HomeworkDto'yu döner, listeyi en baştan yenile.
        await loadHomeworks();
        e.target.reset();
        document.getElementById('hw-due').valueAsDate = defaultDue();

        el.textContent = '✅ Ödev başarıyla gönderildi!';
        el.className = 'mt-3 text-center fw-bold text-success';
    } catch (err) {
        console.error('Ödev gönderilemedi', err);
        el.textContent = '❌ Ödev gönderilemedi. Lütfen tekrar deneyin.';
        el.className = 'mt-3 text-center fw-bold text-danger';
    }

    setTimeout(() => el.className = 'mt-3 text-center fw-medium d-none', 3000);
});

// Yardımcılar
function formatDate(d) {
    if (!d) return '';
    return new Date(d).toLocaleDateString('tr-TR', {day:'2-digit', month:'short', year:'numeric'});
}

function defaultDue() {
    const d = new Date(); d.setDate(d.getDate() + 3); return d;
}

// INIT
async function loadHomeworks() {
    try {
        const data = await apiGet('/TeacherPanelProxy/Homeworks');
        sentItems = Array.isArray(data) ? data : [];
        sentItems.forEach(item => {
            const saved = localStorage.getItem('hw_completedBy_' + item.id);
            if (saved) {
                try { item.completedBy = JSON.parse(saved); } catch(e){}
            }
        });
        renderSentList();
    } catch (err) {
        console.error('Ödevler yüklenemedi', err);
    }
}

(async function() {
    document.getElementById('hw-due').valueAsDate = defaultDue();
    await loadClassrooms();
    await refreshStudentDropdown();
    await loadHomeworks();
})();

// ===================================================
// ÖDEV KONTROL MODALI
// ===================================================
let currentCheckId = null;

async function getStudentsForItemAsync(item) {
    if (item.classroomId) {
        return await loadStudentsForClassroom(item.classroomId);
    } else if (item.userId) {
        return [{ id: item.userId, name: item.userName || 'Bireysel Öğrenci', cls: 'Tekil' }];
    }
    return [];
}

async function openCheckModal(hwId) {
    currentCheckId = hwId;
    const item = sentItems.find(x => x.id === hwId);
    if (!item) return;

    document.getElementById('check-modal-title').textContent = item.topic || item.title || 'Ödev Kontrol';
    document.getElementById('check-modal-subtitle').textContent = "Öğrenciler yükleniyor...";
    const body = document.getElementById('check-modal-body');
    body.innerHTML = '<div class="text-center text-muted py-4"><i class="bi bi-arrow-repeat fs-3 d-block mb-2 opacity-50 spin"></i>Yükleniyor...</div>';
    document.getElementById('check-modal-overlay').classList.add('active');

    const students = await getStudentsForItemAsync(item);
    const completed = item.completedBy || [];

    document.getElementById('check-modal-subtitle').textContent =
        `${item.subject} • ${item.classroomName ? item.classroomName + ' Sınıfı' : item.userName || 'Bireysel'}`;

    body.innerHTML = students.map(s => {
        const checked = completed.includes(s.id) ? 'checked' : '';
        return `
        <label class="check-student-row">
            <input type="checkbox" value="${s.id}" ${checked} />
            <span class="check-student-name">${s.name}</span>
            <span class="check-student-cls">${s.cls}</span>
        </label>`;
    }).join('');

    if (!students.length) {
        body.innerHTML = '<div class="text-center text-muted py-4"><i class="bi bi-people fs-3 d-block mb-2 opacity-50"></i>Öğrenci bulunamadı.</div>';
    }
}

function closeCheckModal() {
    document.getElementById('check-modal-overlay').classList.remove('active');
    currentCheckId = null;
}

function saveCheckModal() {
    if (!currentCheckId) return;
    const item = sentItems.find(x => x.id === currentCheckId);
    if (!item) return;

    const checkboxes = document.querySelectorAll('#check-modal-body input[type=checkbox]');
    item.completedBy = [];
    checkboxes.forEach(cb => {
        if (cb.checked) item.completedBy.push(parseInt(cb.value));
    });

    localStorage.setItem('hw_completedBy_' + item.id, JSON.stringify(item.completedBy));
    // For student frontend simulation
    localStorage.setItem('hw_completed_student_mock_' + item.id, item.completedBy.length > 0 ? 'true' : 'false');

    renderSentList();
    closeCheckModal();
}

function collectStudentsForSnapshotPdf(item) {
    const rows = document.querySelectorAll('#check-modal-body label.check-student-row');
    const out = [];
    rows.forEach(label => {
        const cb = label.querySelector('input[type=checkbox]');
        const nameEl = label.querySelector('.check-student-name');
        if (!nameEl) return;
        out.push({ name: nameEl.textContent.trim(), completed: !!(cb && cb.checked) });
    });
    if (out.length === 0 && item.userId) {
        const done = (item.completedBy || []).includes(item.userId);
        out.push({ name: (item.userName || 'Öğrenci').trim(), completed: done });
    }
    return out;
}

function openHomeworkSnapshotPdf() {
    const item = sentItems.find(x => x.id === currentCheckId);
    if (!item) return;
    const target = item.classroomName
        ? `${item.classroomName} sınıfı`
        : (item.userName || 'Bireysel öğrenci');
    // noopener: bazı tarayıcılarda opener pencerenin blob URL'ine yönlendirmesi çalışmıyor; boş about:blank kalıyor.
    const pdfWindow = window.open('about:blank', '_blank');
    if (!pdfWindow) {
        alert('Tarayıcı yeni sekmeyi engelledi. Lütfen açılır pencerelere izin verin.');
        return;
    }
    try {
        pdfWindow.document.write(
            '<!DOCTYPE html><html><head><meta charset="utf-8"><title>PDF</title></head>' +
            '<body style="margin:0;font-family:system-ui,sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;background:#f8fafc;color:#64748b">' +
            '<p>PDF hazırlanıyor…</p></body></html>');
        pdfWindow.document.close();
    } catch (e) { /* yine de fetch dene */ }

    fetch(window.__teacherHomeworkSnapshotPdfUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept': 'application/pdf' },
        body: JSON.stringify({
            topic: item.topic || item.title || '',
            subject: item.subject || '',
            dueDate: item.dueDate,
            description: item.description || '',
            target: target,
            students: collectStudentsForSnapshotPdf(item)
        })
    })
        .then(async res => {
            if (!res.ok) {
                pdfWindow.close();
                alert('PDF oluşturulamadı. Oturumunuzun açık olduğundan emin olun.');
                return;
            }
            const buf = await res.arrayBuffer();
            const blob = new Blob([buf], { type: 'application/pdf' });
            const url = URL.createObjectURL(blob);
            try {
                pdfWindow.document.open();
                pdfWindow.document.write(
                    '<!DOCTYPE html><html><head><meta charset="utf-8"><title>Ödev özeti</title>' +
                    '<style>html,body{margin:0;height:100%}iframe{border:0;width:100%;height:100%}</style></head><body>' +
                    '<iframe src="' + url.replace(/"/g, '') + '" title="PDF"></iframe></body></html>');
                pdfWindow.document.close();
            } catch (e) {
                pdfWindow.location.replace(url);
            }
            setTimeout(() => URL.revokeObjectURL(url), 180000);
        })
        .catch(err => {
            console.error(err);
            try { pdfWindow.close(); } catch (e2) { }
            alert('PDF indirilemedi.');
        });
}
