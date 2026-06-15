using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Tutorial
{
    public sealed class TutorialService
    {
        private readonly AppDbContext _db;

        public TutorialService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<int>> GetCompletedTutorialIdsAsync(long userId, CancellationToken ct)
        {
            return await _db.UserTutorialProgress.Query()
                .Where(x => x.UserId == userId)
                .Select(x => x.TutorialId)
                .ToListAsync(ct);
        }

        public async Task<List<int>> CompleteTutorialAsync(long userId, int tutorialId, CancellationToken ct)
        {
            var existing = await _db.UserTutorialProgress.FindAsync(userId, tutorialId, ct);
            if (existing == null)
            {
                var row = new UserTutorialProgressRow
                {
                    UserId = userId,
                    TutorialId = tutorialId,
                    ViewedAt = DateTimeOffset.UtcNow
                };
                _db.UserTutorialProgress.Insert(row);
                await _db.SaveAsync(ct);
            }

            return await GetCompletedTutorialIdsAsync(userId, ct);
        }
    }
}
