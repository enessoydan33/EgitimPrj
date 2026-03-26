using System;
using System.Collections.Generic;

namespace EgitimPrj.Models.Response
{
    public enum LeagueType
    {
        General,
        School,
        Grade
    }

    public class LeaderboardItemDto
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? SchoolName { get; set; }
        public int? GradeLevel { get; set; }
        public int TotalXp { get; set; }
        public int CurrentStreak { get; set; }
        public bool IsCurrentUser { get; set; }
        
        // Lig bilgileri
        public int? LeagueId { get; set; }
        public string? LeagueName { get; set; }
        public string? LeagueIcon { get; set; }
        public string? LeagueColor { get; set; }
    }

    public class LeaderboardResponse
    {
        public string LeagueType { get; set; } = "General";
        public string? LeagueName { get; set; }
        public List<LeaderboardItemDto> Rankings { get; set; } = new();
        public LeaderboardItemDto? CurrentUserRank { get; set; }
        public int TotalUsers { get; set; }
    }

    public class AllLeaguesResponse
    {
        public LeaderboardResponse General { get; set; } = new();
        public LeaderboardResponse? School { get; set; }
        public LeaderboardResponse? Grade { get; set; }
    }

    public class XpTransactionDto
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public string SourceType { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime EarnedAt { get; set; }
    }

    public class UserXpSummaryDto
    {
        public int UserId { get; set; }
        public int TotalXp { get; set; }
        public int TodayXp { get; set; }
        public int WeekXp { get; set; }
        public int MonthXp { get; set; }
        public int CurrentStreak { get; set; }
        public GeneralRankDto GeneralRank { get; set; } = new();
        public SchoolRankDto? SchoolRank { get; set; }
        public GradeRankDto? GradeRank { get; set; }
        
        // Lig bilgileri
        public UserLeagueInfoDto? LeagueInfo { get; set; }
    }

    public class GeneralRankDto
    {
        public int Rank { get; set; }
        public int TotalUsers { get; set; }
    }

    public class SchoolRankDto
    {
        public int Rank { get; set; }
        public int TotalUsers { get; set; }
        public string SchoolName { get; set; } = null!;
    }

    public class GradeRankDto
    {
        public int Rank { get; set; }
        public int TotalUsers { get; set; }
        public int GradeLevel { get; set; }
        public string GradeName { get; set; } = null!;
    }

    /// <summary>
    /// Kullanıcının lig bilgisi özeti
    /// </summary>
    public class UserLeagueInfoDto
    {
        public int LeagueId { get; set; }
        public string LeagueName { get; set; } = null!;
        public string? LeagueIcon { get; set; }
        public string? LeagueColor { get; set; }
        public int RankInLeague { get; set; }
        public int TotalUsersInLeague { get; set; }
        public int XpToNextLeague { get; set; }
        public double ProgressPercentage { get; set; }
        public string? NextLeagueName { get; set; }
    }
}











