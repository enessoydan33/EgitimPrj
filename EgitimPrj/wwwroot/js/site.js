(() => {
  'use strict';

  const safeHtml = (value) => {
    const s = String(value ?? '');
    return s
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  };

  const fetchJson = async (url, options = undefined) => {
    const res = await fetch(url, options);
    const text = await res.text();
    if (!res.ok) {
      const suffix = text ? `: ${text}` : '';
      throw new Error(`HTTP ${res.status}${suffix}`);
    }
    const trimmed = (text || '').trim();
    if (!trimmed)
      return null;
    try {
      return JSON.parse(trimmed);
    } catch (e) {
      throw new Error(`Geçersiz JSON yanıtı: ${e?.message || e}`);
    }
  };

  const getAntiForgeryToken = () =>
    document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

  const setText = (idOrEl, value) => {
    const el = typeof idOrEl === 'string' ? document.getElementById(idOrEl) : idOrEl;
    if (!el) return;
    el.textContent = value ?? '';
  };

  const setVisible = (idOrEl, visible, display = 'block') => {
    const el = typeof idOrEl === 'string' ? document.getElementById(idOrEl) : idOrEl;
    if (!el) return;
    el.style.display = visible ? display : 'none';
  };

  const normalizeAchievements = (data) => {
    if (Array.isArray(data)) return data;
    const list = data?.data || data?.items || data?.achievements || data?.Achievements || data?.$values || [];
    return Array.isArray(list) ? list : [];
  };

  /**
   * Haftalık program: API ya DailyScheduleDto[] (schedules içeren günler) ya da düz StudyScheduleDto[] dönebilir.
   */
  const normalizeWeeklySchedulePayload = (data) => {
    if (!data || !Array.isArray(data) || data.length === 0) return [];
    const first = data[0];
    if (first && (Array.isArray(first.schedules) || Array.isArray(first.Schedules))) return data;
    const byDay = new Map();
    for (const s of data) {
      const dow = Number(s.dayOfWeek ?? s.DayOfWeek ?? 0);
      if (!byDay.has(dow)) byDay.set(dow, { dayOfWeek: dow, schedules: [] });
      byDay.get(dow).schedules.push(s);
    }
    return Array.from(byDay.values()).sort((a, b) => a.dayOfWeek - b.dayOfWeek);
  };

  const apiRequest = async (url, {
    method = 'GET',
    body = undefined,
    headers = {},
    includeJsonContentType = true,
    includeAntiForgery = false,
  } = {}) => {
    const finalHeaders = { ...headers };
    if (includeJsonContentType && !finalHeaders['Content-Type']) {
      finalHeaders['Content-Type'] = 'application/json';
    }
    if (includeAntiForgery) {
      finalHeaders['RequestVerificationToken'] = getAntiForgeryToken();
    }

    return fetch(url, {
      method,
      headers: finalHeaders,
      body: body !== undefined
        ? (typeof body === 'string' ? body : JSON.stringify(body))
        : undefined,
    });
  };

  const togglePassword = (inputId, iconId) => {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(iconId);
    if (!input || !icon) return;

    if (input.type === 'password') {
      input.type = 'text';
      icon.className = 'bi bi-eye-slash';
    } else {
      input.type = 'password';
      icon.className = 'bi bi-eye';
    }
  };

  window.AppUtils = Object.freeze({
    safeHtml,
    fetchJson,
    getAntiForgeryToken,
    apiRequest,
    setText,
    setVisible,
    normalizeAchievements,
    normalizeWeeklySchedulePayload,
    togglePassword,
  });

  // Page modules (optional initializers)
  window.AppPages = window.AppPages || {};

  window.AppPages.Schedule = (() => {
    const DAY_NAMES = ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'];
    const ORDERED_DAYS = [1, 2, 3, 4, 5, 6, 0];
    const START_HOUR = 7;
    const END_HOUR = 23;

    const isScheduleDomPresent = () =>
      !!document.getElementById('weekBody') &&
      !!document.getElementById('addSubject') &&
      !!document.getElementById('schedToast');

    const init = () => {
      if (!isScheduleDomPresent()) return;

      let subjectsMap = {};
      let allScheduleData = [];
      let currentView = 'grid';

      const hex2rgb = (hex) => {
        if (!hex) return '99,102,241';
        const r = parseInt(hex.slice(1, 3), 16);
        const g = parseInt(hex.slice(3, 5), 16);
        const b = parseInt(hex.slice(5, 7), 16);
        return `${r},${g},${b}`;
      };

      const timeToMinutes = (t) => {
        if (!t) return 0;
        const [h, m] = t.substring(0, 5).split(':').map(Number);
        return h * 60 + (m || 0);
      };

      const fmtTime = (t) => (t || '').substring(0, 5);

      const normalizeTimeForApi = (t) => {
        const v = (t || '').trim();
        if (!v) return '';
        if (/^\d{2}:\d{2}$/.test(v)) return `${v}:00`;
        if (/^\d{2}:\d{2}:\d{2}$/.test(v)) return v;
        return v;
      };

      const apiRequest = (url, method, body, includeJsonHeader = true) =>
        window.AppUtils.apiRequest(url, {
          method,
          body,
          includeJsonContentType: includeJsonHeader,
          includeAntiForgery: true,
        });

      const showToast = (msg, type = 'success') => {
        const el = document.getElementById('schedToast');
        const icon = document.getElementById('toastIcon');
        window.AppUtils.setText('toastMsg', msg);
        el.className = `sched-toast toast-${type}`;
        icon.className = `bi ${type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-circle-fill'}`;
        setTimeout(() => el.classList.add('show'), 10);
        setTimeout(() => el.classList.remove('show'), 3000);
      };

      const populateTimeSlots = () => {
        const sel = document.getElementById('addTimeSlot');
        if (!sel) return;
        sel.innerHTML = '';
        for (let h = 7; h < 23; h++) {
          const st = `${String(h).padStart(2, '0')}:00`;
          const en = `${String(h + 1).padStart(2, '0')}:00`;
          const opt = document.createElement('option');
          opt.value = `${st}|${en}`;
          opt.textContent = `${st} – ${en}`;
          sel.appendChild(opt);
        }
      };

      const loadSubjects = async () => {
        try {
          const res = await fetch('/Exam/Subjects');
          if (!res.ok) throw new Error('Subjects load failed');
          const subjects = await res.json();

          const addSel = document.getElementById('addSubject');
          const editSel = document.getElementById('editSubject');
          if (addSel) addSel.innerHTML = '<option value="">Bir ders seçin...</option>';
          if (editSel) editSel.innerHTML = '<option value="">Bir ders seçin...</option>';

          subjectsMap = {};
          subjects.forEach((s) => {
            const id = s.id || s.Id;
            const name = s.name || s.Name;
            const color = s.colorHex || s.ColorHex || '#6366f1';
            subjectsMap[id] = { name, color };

            [addSel, editSel].forEach((sel) => {
              if (!sel) return;
              const opt = document.createElement('option');
              opt.value = id;
              opt.textContent = name;
              sel.appendChild(opt);
            });
          });
        } catch (e) {
          console.warn('Dersler yüklenemedi', e);
        }
      };

      const showLoader = (show) => {
        document.getElementById('calLoading')?.classList.toggle('d-none', !show);
        document.getElementById('calGrid')?.classList.add('d-none');
        document.getElementById('calList')?.classList.add('d-none');
        document.getElementById('calEmpty')?.classList.add('d-none');
      };

      const updateStats = () => {
        const totalItems = allScheduleData.length;
        let totalMinutes = 0;
        const activeDays = new Set();

        allScheduleData.forEach((ev) => {
          totalMinutes += timeToMinutes(ev.endTime) - timeToMinutes(ev.startTime);
          activeDays.add(ev.dayOfWeek);
        });

        window.AppUtils.setText('statTotalItems', totalItems);
        window.AppUtils.setText(
          'statTotalHours',
          `${(totalMinutes / 60).toFixed(1).replace('.0', '')} sa`,
        );
        window.AppUtils.setText('statActiveDays', activeDays.size);
      };

      const findEventById = (id) => allScheduleData.find((e) => String(e.id) === String(id));

      const renderList = () => {
        document.getElementById('calList')?.classList.remove('d-none');
        document.getElementById('calGrid')?.classList.add('d-none');

        const grouped = {};
        ORDERED_DAYS.forEach((d) => { grouped[d] = []; });
        allScheduleData.forEach((ev) => {
          if (grouped[ev.dayOfWeek] !== undefined) grouped[ev.dayOfWeek].push(ev);
        });

        let html = '';
        ORDERED_DAYS.forEach((dayIdx) => {
          const items = grouped[dayIdx];
          if (!items || !items.length) return;
          items.sort((a, b) => (a.startTime || '').localeCompare(b.startTime || ''));
          html += `<div class="list-day-block">
            <div class="list-day-title">${DAY_NAMES[dayIdx]}</div>`;

          items.forEach((ev) => {
            const subj = subjectsMap[ev.subjectId] || { name: ev.subjectName || 'Etüt', color: '#6366f1' };
            const color = ev.subjectColor || subj.color || '#6366f1';
            html += `
            <div class="list-event-item" style="border-left-color:${color}" onclick="openEditModal(findEventById(${ev.id}))">
                <div class="list-event-dot" style="background:${color}"></div>
                <div class="list-ev-time">${fmtTime(ev.startTime)} – ${fmtTime(ev.endTime)}</div>
                <div>
                    <div class="list-ev-name">${subj.name}</div>
                    ${ev.topic ? `<div class="list-ev-topic">${ev.topic}</div>` : ''}
                </div>
                <div class="list-ev-edit" onclick="event.stopPropagation(); openEditModal(findEventById(${ev.id}))" title="Düzenle">
                    <i class="bi bi-pencil" style="font-size:11px"></i>
                </div>
                <div class="list-ev-del" onclick="event.stopPropagation(); deleteItem(event, ${ev.id})" title="Sil">
                    <i class="bi bi-x-lg" style="font-size:11px"></i>
                </div>
            </div>`;
          });

          html += '</div>';
        });

        const listContent = document.getElementById('listContent');
        if (listContent) {
          listContent.innerHTML = html || '<div class="p-4 text-center text-muted">Program boş.</div>';
        }
      };

      const renderGrid = () => {
        document.getElementById('calGrid')?.classList.remove('d-none');
        document.getElementById('calList')?.classList.add('d-none');

        const today = new Date().getDay();

        const headerEl = document.getElementById('weekHeader');
        if (headerEl) {
          headerEl.innerHTML = '<div class="week-time-label" style="border-bottom:none"></div>';
          ORDERED_DAYS.forEach((d) => {
            const isToday = d === today;
            const div = document.createElement('div');
            div.className = `week-header-day${isToday ? ' today' : ''}`;
            div.innerHTML = `
              <div class="day-name">${DAY_NAMES[d].substring(0, 3).toUpperCase()}</div>
              <div class="day-date">${DAY_NAMES[d].substring(0, 3)}</div>`;
            headerEl.appendChild(div);
          });
        }

        const evMap = {};
        allScheduleData.forEach((ev) => {
          const key = `${ev.dayOfWeek}`;
          if (!evMap[key]) evMap[key] = [];
          evMap[key].push(ev);
        });

        const bodyEl = document.getElementById('weekBody');
        if (!bodyEl) return;
        bodyEl.innerHTML = '';

        for (let h = START_HOUR; h < END_HOUR; h++) {
          const row = document.createElement('div');
          row.className = 'week-row';
          const lbl = document.createElement('div');
          lbl.className = 'week-time-label';
          lbl.textContent = `${String(h).padStart(2, '0')}:00`;
          row.appendChild(lbl);

          ORDERED_DAYS.forEach((dayIdx) => {
            const cell = document.createElement('div');
            cell.className = 'week-cell';
            const dayEvents = (evMap[dayIdx] || []).filter((ev) => {
              const evH = parseInt((ev.startTime || '').substring(0, 2), 10);
              return evH === h;
            });

            dayEvents.forEach((ev) => {
              const subj = subjectsMap[ev.subjectId] || { name: ev.subjectName || 'Etüt', color: '#6366f1' };
              const color = ev.subjectColor || subj.color || '#6366f1';
              const rgb = hex2rgb(color);

              const evEl = document.createElement('div');
              evEl.className = `cal-event${ev.isAI ? ' ai-event' : ''}`;
              evEl.style.background = `rgba(${rgb},.85)`;
              evEl.style.borderLeft = `3px solid rgba(${rgb},1)`;
              evEl.innerHTML = `
                  <div class="ev-time">${fmtTime(ev.startTime)}–${fmtTime(ev.endTime)}</div>
                  <div class="ev-title">${subj.name}</div>
                  ${ev.topic ? `<div class="ev-time">${ev.topic}</div>` : ''}
                  <div class="ev-del" onclick="deleteItem(event, ${ev.id})"><i class="bi bi-x"></i></div>`;

              evEl.addEventListener('click', (e) => {
                if (e.target.closest('.ev-del')) return;
                window.openEditModal(ev);
              });

              cell.appendChild(evEl);
            });

            row.appendChild(cell);
          });

          bodyEl.appendChild(row);
        }
      };

      const renderCalendar = () => {
        showLoader(false);
        if (allScheduleData.length === 0) {
          document.getElementById('calEmpty')?.classList.remove('d-none');
          return;
        }
        if (currentView === 'grid') renderGrid();
        else renderList();
      };

      const loadSchedule = async () => {
        showLoader(true);
        try {
          const res = await fetch('/Pomodoro/WeeklySchedule');
          if (!res.ok) throw new Error(await res.text());
          const data = await res.json();

          const flat = [];
          (data || []).forEach((day) => {
            const dayIdx = day.dayOfWeek ?? day.DayOfWeek;
            const schedules = day.schedules || day.Schedules || [];
            schedules.forEach((item) => {
              flat.push({
                id: item.id ?? item.Id,
                subjectId: item.subjectId ?? item.SubjectId,
                subjectName: item.subjectName ?? item.SubjectName ?? '',
                subjectColor: item.subjectColorHex ?? item.SubjectColorHex ?? null,
                dayOfWeek: item.dayOfWeek ?? item.DayOfWeek ?? dayIdx,
                startTime: item.startTime ?? item.StartTime ?? '',
                endTime: item.endTime ?? item.EndTime ?? '',
                topic: item.topic ?? item.Topic ?? '',
                isAI: false,
              });
            });
          });

          allScheduleData = flat;
          renderCalendar();
          updateStats();
        } catch (e) {
          console.error(e);
          showLoader(false);
          document.getElementById('calEmpty')?.classList.remove('d-none');
        }
      };

      // -------------------------------
      // Global functions used by HTML
      // -------------------------------
      window.findEventById = findEventById;

      window.toggleView = (v) => {
        currentView = v;
        const btnGrid = document.getElementById('btnGridView');
        const btnList = document.getElementById('btnListView');
        if (btnGrid) {
          btnGrid.className = v === 'grid' ? 'btn btn-sm btn-primary' : 'btn btn-sm btn-outline-primary';
        }
        if (btnList) {
          btnList.className = v === 'list' ? 'btn btn-sm btn-secondary' : 'btn btn-sm btn-outline-secondary';
        }
        if (allScheduleData.length > 0) renderCalendar();
      };

      window.addNewLesson = async () => {
        const subjectId = document.getElementById('addSubject')?.value;
        const dayOfWeek = parseInt(document.getElementById('addDay')?.value || '', 10);
        const timeSlot = document.getElementById('addTimeSlot')?.value;
        const topic = (document.getElementById('addTopic')?.value || '').trim();

        if (!subjectId) { showToast('Lütfen bir ders seçin.', 'error'); return; }
        if (!timeSlot) { showToast('Lütfen saat aralığı seçin.', 'error'); return; }

        const [rawStartTime, rawEndTime] = timeSlot.split('|');
        const startTime = normalizeTimeForApi(rawStartTime);
        const endTime = normalizeTimeForApi(rawEndTime);

        if (Number.isNaN(dayOfWeek) || dayOfWeek < 0 || dayOfWeek > 6) {
          showToast('Geçerli bir gün seçin.', 'error');
          return;
        }
        if (!startTime || !endTime || timeToMinutes(endTime) <= timeToMinutes(startTime)) {
          showToast('Saat aralığı geçersiz.', 'error');
          return;
        }

        try {
          const res = await apiRequest('/Pomodoro/CreateSchedule', 'POST', {
            subjectId: parseInt(subjectId, 10),
            dayOfWeek,
            startTime,
            endTime,
            topic: topic || null,
          });
          if (res.ok) {
            showToast('Ders başarıyla eklendi!');
            const addTopic = document.getElementById('addTopic');
            if (addTopic) addTopic.value = '';
            await loadSchedule();
          } else {
            const errText = (await res.text()) || 'Eklenirken hata oluştu.';
            showToast(errText, 'error');
          }
        } catch (ex) {
          console.error(ex);
          showToast('Bağlantı hatası!', 'error');
        }
      };

      window.openEditModal = (ev) => {
        if (!ev) return;
        const overlay = document.getElementById('editModalOverlay');
        document.getElementById('editId').value = ev.id;
        document.getElementById('editSubject').value = ev.subjectId || '';
        document.getElementById('editDay').value = ev.dayOfWeek;
        document.getElementById('editStartTime').value = fmtTime(ev.startTime);
        document.getElementById('editEndTime').value = fmtTime(ev.endTime);
        document.getElementById('editTopic').value = ev.topic || '';
        overlay?.classList.add('active');
      };

      window.closeEditModal = () => {
        document.getElementById('editModalOverlay')?.classList.remove('active');
      };

      window.saveEdit = async () => {
        const id = document.getElementById('editId')?.value;
        const subjectId = parseInt(document.getElementById('editSubject')?.value || '', 10);
        const dayOfWeek = parseInt(document.getElementById('editDay')?.value || '', 10);
        const startTime = normalizeTimeForApi(document.getElementById('editStartTime')?.value);
        const endTime = normalizeTimeForApi(document.getElementById('editEndTime')?.value);
        const topic = (document.getElementById('editTopic')?.value || '').trim();

        if (!subjectId) { showToast('Lütfen bir ders seçin.', 'error'); return; }
        if (!startTime || !endTime) { showToast('Saat aralığı boş olamaz.', 'error'); return; }
        if (Number.isNaN(dayOfWeek) || dayOfWeek < 0 || dayOfWeek > 6) { showToast('Geçerli bir gün seçin.', 'error'); return; }
        if (timeToMinutes(endTime) <= timeToMinutes(startTime)) { showToast('Bitiş saati başlangıçtan büyük olmalı.', 'error'); return; }

        try {
          const res = await apiRequest(`/Pomodoro/UpdateSchedule?scheduleId=${encodeURIComponent(id)}`, 'PUT', {
            subjectId,
            dayOfWeek,
            startTime,
            endTime,
            topic: topic || null,
          });

          if (res.ok) {
            showToast('Ders güncellendi!');
            window.closeEditModal();
            await loadSchedule();
          } else {
            const errText = (await res.text()) || 'Güncellenirken hata oluştu.';
            showToast(errText, 'error');
          }
        } catch (ex) {
          console.error(ex);
          showToast('Bağlantı hatası!', 'error');
        }
      };

      window.deleteFromModal = () => {
        const id = document.getElementById('editId')?.value;
        if (!id) return;
        window.closeEditModal();
        window.deleteItem(new Event('click'), id);
      };

      window.deleteItem = async (e, id) => {
        if (e && e.stopPropagation) e.stopPropagation();
        if (!confirm('Bu etüdü silmek istiyor musunuz?')) return;

        try {
          const res = await apiRequest(`/Pomodoro/DeleteSchedule?scheduleId=${id}`, 'DELETE', undefined, false);
          if (res.ok) {
            showToast('Ders silindi.');
            await loadSchedule();
          } else {
            showToast('Silinirken hata oluştu.', 'error');
          }
        } catch (ex) {
          console.error(ex);
          showToast('Bağlantı hatası!', 'error');
        }
      };

      document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') window.closeEditModal();
      }, { passive: true });

      // Init run
      populateTimeSlots();
      window.toggleView('grid');
      loadSubjects().then(() => loadSchedule());
    };

    return { init };
  })();

  // Backward-compatible global for inline onclick handlers
  window.togglePassword = togglePassword;
})();
