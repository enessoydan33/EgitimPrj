using EgitimPrj.Models.ViewModel.ExamViewModel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace EgitimPrj.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Eğer öğretmen girişi varsa direkt öğretmen paneline yönlendir
            var isTeacherLoggedIn = HttpContext.Session.GetString("IsTeacherLoggedIn");
            if (string.Equals(isTeacherLoggedIn, "true", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("TeacherPanel", "Teacher");
            }

            // Ana dashboard verilerini burada hazırlayabiliriz
            ViewData["Title"] = "Ana Sayfa";
            return View();
        }

        public IActionResult AIWork()
        {
            ViewData["Title"] = "Yapay Zeka ile Çalışma";
            return View();
        }

        public IActionResult Lessons()
        {
            ViewData["Title"] = "Dersler";
            return View();
        }

        public IActionResult ParentSummary()
        {
            ViewData["Title"] = "Veli Bilgilendirme";
            return View();
        }

        public IActionResult ExamTracking()
        {
            ViewData["Title"] = "Deneme Takibi";

            var model = new CreateExamViewModel
            {
                ExamDate = DateTime.Today,
                SubjectScores = new List<SubjectScoreViewModel>
                {
                    new()
                }
            };

            return View(model);
        }

        public IActionResult AIProgramHazirlama()
        {
            ViewData["Title"] = "Program Hazırlama";
            return View();
        }

        // Deneme analizi ve deneme takibi tek sayfada birleşti.
        // Eski DenemeAnalizi adresine giden kullanıcıları ExamTracking sayfasına yönlendiriyoruz.
        public IActionResult DenemeAnalizi()
        {
            return RedirectToAction("ExamTracking");
        }

        public IActionResult BasariTahmini()
        {
            ViewData["Title"] = "Basari Tahmini";
            return View();
        }

        public IActionResult Leaderboard()
        {
            ViewData["Title"] = "Liderlik Tablosu";
            return View();
        }

        public IActionResult Schedule()
        {
            ViewData["Title"] = "Randevu Takvimi";
            return View();
        }

        public IActionResult Appointments()
        {
            ViewData["Title"] = "Randevu";
            return View();
        }
    }
}
