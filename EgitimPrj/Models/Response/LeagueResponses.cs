using System;
using System.Collections.Generic;

namespace EgitimPrj.Models.Response
{
    /// <summary>
    /// Lig bilgisi DTO
    /// </summary>
    public class LeagueDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int MinXp { get; set; }
        public int? MaxXp { get; set; }
        public string? LeagueColor { get; set; }
        public string? Icon { get; set; }
        public int RankOrder { get; set; }
    }

    /// <summary>
    /// Kullanıcının mevcut lig durumu
    /// </summary>
    public class UserLeagueDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public int TotalXp { get; set; }

        // Mevcut lig bilgileri
        public LeagueDto CurrentLeague { get; set; } = null!;

        // Bir sonraki lig bilgileri (varsa)
        public LeagueDto? NextLeague { get; set; }

        // İlerleme bilgileri
        public int XpInCurrentLeague { get; set; }
        public int XpToNextLeague { get; set; }
        public double ProgressPercentage { get; set; }

        // Lig içi sıralama
        public int RankInLeague { get; set; }
        public int TotalUsersInLeague { get; set; }
    }

    /// <summary>
    /// Lig geçmişi DTO
    /// </summary>
    public class UserLeagueHistoryDto
    {
        public int Id { get; set; }
        public LeagueDto League { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? FinalXp { get; set; }
        public int? FinalRank { get; set; }
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// Lig sıralaması - Belirli bir ligteki kullanıcılar
    /// </summary>
    public class LeagueLeaderboardDto
    {
        public LeagueDto League { get; set; } = null!;
        public List<LeagueLeaderboardItemDto> Rankings { get; set; } = new();
        public int TotalUsers { get; set; }
        public LeagueLeaderboardItemDto? CurrentUserRank { get; set; }
    }

    /// <summary>
    /// Lig sıralaması item
    /// </summary>
    public class LeagueLeaderboardItemDto
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int TotalXp { get; set; }
        public int CurrentStreak { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    /// <summary>
    /// Tüm ligler ve kullanıcı sayıları
    /// </summary>
    public class AllLeaguesOverviewDto
    {
        public List<LeagueOverviewItemDto> Leagues { get; set; } = new();
        public int? CurrentUserLeagueId { get; set; }
    }

    /// <summary>
    /// Lig özeti item
    /// </summary>
    public class LeagueOverviewItemDto
    {
        public LeagueDto League { get; set; } = null!;
        public int TotalUsers { get; set; }
        public bool IsCurrentUserLeague { get; set; }
    }

    /// <summary>
    /// Lig değişikliği bilgisi
    /// </summary>
    public class LeagueChangeDto
    {
        public bool HasChanged { get; set; }
        public LeagueDto? PreviousLeague { get; set; }
        public LeagueDto? NewLeague { get; set; }
        public string? Message { get; set; }
        public bool IsPromotion { get; set; }
    }
}














