﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.Api.Models;
using AntiPlagiarism.Web.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AntiPlagiarism.Web.Database.Repos
{
	public interface ISubmissionsRepo
	{
		Task<List<Submission>> GetSubmissionsAsync(int startFromIndex, int maxCount);
		Task<Submission> FindSubmissionByIdAsync(int submissionId);
		Task<List<Submission>> GetSubmissionsByIdsAsync(IEnumerable<int> submissionIds);
		Task<Submission> AddSubmissionAsync(int clientId, Guid taskId, Guid authorId, Language language, string code, int tokensCount, string additionalInfo);
		Task UpdateSubmissionTokensCountAsync(Submission submission, int tokensCount);
		Task<List<Submission>> GetSubmissionsByAuthorAndTaskAsync(int clientId, Guid authorId, Guid taskId, int count);
		Task<List<Guid>> GetLastAuthorsByTaskAsync(int clientId, Guid taskId, int count);
		Task<List<Submission>> GetLastSubmissionsByAuthorsForTaskAsync(int clientId, Guid taskId, IEnumerable<Guid> authorsIds);
		Task<int> GetAuthorsCountAsync(int clientId, Guid taskId);
		Task<List<Submission>> GetSubmissionsByTaskAsync(int clientId, Guid taskId);
		Task<int> GetSubmissionsCountAsync(int clientId, Guid taskId);
	}

	public class SubmissionsRepo : ISubmissionsRepo
	{
		private readonly AntiPlagiarismDb db;

		public SubmissionsRepo(AntiPlagiarismDb db)
		{
			this.db = db;
		}

		public async Task<List<Submission>> GetSubmissionsAsync(int startFromIndex, int maxCount)
		{
			return await db.Submissions.Include(s => s.Program)
				.Where(s => s.Id >= startFromIndex)
				.OrderBy(s => s.Id)
				.Take(maxCount)
				.ToListAsync();
		}

		public Task<Submission> FindSubmissionByIdAsync(int submissionId)
		{
			return db.Submissions.Include(s => s.Program).FirstOrDefaultAsync(s => s.Id == submissionId);
		}

		public Task<List<Submission>> GetSubmissionsByIdsAsync(IEnumerable<int> submissionIds)
		{
			return db.Submissions.Include(s => s.Program).Where(s => submissionIds.Contains(s.Id)).ToListAsync();
		}

		public async Task<Submission> AddSubmissionAsync(int clientId, Guid taskId, Guid authorId, Language language, string code, int tokensCount, string additionalInfo = "")
		{
			var submission = new Submission
			{
				ClientId = clientId,
				TaskId = taskId,
				AuthorId = authorId,
				Language = language,
				Program = new Code
				{
					Text = code,
				},
				TokensCount = tokensCount,
				AdditionalInfo = additionalInfo,
				AddingTime = DateTime.Now,
			};

			db.Submissions.Add(submission);
			await db.SaveChangesAsync();

			return submission;
		}

		public Task UpdateSubmissionTokensCountAsync(Submission submission, int tokensCount)
		{
			submission.TokensCount = tokensCount;
			return db.SaveChangesAsync();
		}

		public Task<List<Guid>> GetLastAuthorsByTaskAsync(int clientId, Guid taskId, int count)
		{
			return db.Submissions
				.OrderByDescending(s => s.Id)
				.Where(s => s.ClientId == clientId && s.TaskId == taskId)
				.Select(s => s.AuthorId)
				.Distinct()
				.Take(count)
				.ToListAsync();
		}

		// https://www.thinktecture.com/en/entity-framework-core/hidden-group-by-capabilities-in-3-0-part-2/
		public async Task<List<Submission>> GetLastSubmissionsByAuthorsForTaskAsync(int clientId, Guid taskId, IEnumerable<Guid> authorsIds)
		{
			var lastSubmissionByAuthor = await db.Submissions
				.Where(s => s.ClientId == clientId && s.TaskId == taskId && authorsIds.Contains(s.AuthorId))
				.Select(s => s.AuthorId)
				.Distinct()
				.Select(authorId => db.Submissions
					.Where(s => s.ClientId == clientId && s.TaskId == taskId && s.AuthorId == authorId)
					.OrderByDescending(p => p.Id)
					.FirstOrDefault()
				)
				.ToListAsync();
			return lastSubmissionByAuthor.ToList();
		}

		public Task<List<Submission>> GetSubmissionsByAuthorAndTaskAsync(int clientId, Guid authorId, Guid taskId, int count)
		{
			return db.Submissions
				.Include(s => s.Program)
				.Where(s => s.ClientId == clientId && s.AuthorId == authorId && s.TaskId == taskId)
				.OrderByDescending(s => s.AddingTime)
				.Take(count)
				.ToListAsync();
		}

		public Task<int> GetAuthorsCountAsync(int clientId, Guid taskId)
		{
			return db.Submissions.Where(s => s.ClientId == clientId && s.TaskId == taskId).Select(s => s.AuthorId).Distinct().CountAsync();
		}

		public Task<List<Submission>> GetSubmissionsByTaskAsync(int clientId, Guid taskId)
		{
			return db.Submissions.Include(s => s.Program).Where(s => s.ClientId == clientId && s.TaskId == taskId).ToListAsync();
		}

		public Task<int> GetSubmissionsCountAsync(int clientId, Guid taskId)
		{
			return db.Submissions.Where(s => s.ClientId == clientId && s.TaskId == taskId).CountAsync();
		}
	}
}