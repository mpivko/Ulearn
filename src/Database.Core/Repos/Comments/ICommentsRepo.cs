﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Database.Models;
using Ulearn.Common;

namespace Database.Repos.Comments
{
	public interface ICommentsRepo
	{
		/// <summary>
		/// Добавляет новый комментарий
		/// </summary>
		/// <param name="authorId">Идентификатор автора</param>
		/// <param name="courseId">Идентификатор курса</param>
		/// <param name="slideId">Идентификатор слайда</param>
		/// <param name="parentCommentId">Идентификатор родительского комментария. -1, если родительского комментарий нет</param>
		/// <param name="commentText">Текст комментария</param>
		/// <returns>Добавленный комментарий</returns>
		Task<Comment> AddCommentAsync(string authorId, string courseId, Guid slideId, int parentCommentId, string commentText);
		
		Task<Comment> FindCommentByIdAsync(int commentId);
		Task<List<Comment>> GetCommentsByIdsAsync(IEnumerable<int> commentIds);
		Task<List<Comment>> GetSlideCommentsAsync(string courseId, Guid slideId);
		Task<List<Comment>> GetSlidesCommentsAsync(string courseId, IEnumerable<Guid> slidesIds);
		Task<List<Comment>> GetCourseCommentsAsync(string courseId);
		Task<Comment> ModifyCommentAsync(int commentId, Action<Comment> modifyAction);
		Task<Comment> EditCommentTextAsync(int commentId, string newText);
		Task<Comment> ApproveCommentAsync(int commentId, bool isApproved);
		Task DeleteCommentAsync(int commentId);
		Task<Comment> RestoreCommentAsync(int commentId);
		Task<Comment> PinCommentAsync(int commentId, bool isPinned);
		Task<Comment> MarkCommentAsCorrectAnswerAsync(int commentId, bool isCorrect = true);
		Task<bool> IsUserAddedMaxCommentsInLastTimeAsync(string userId, int maxCount, TimeSpan lastTime);
		Task<DefaultDictionary<int, List<Comment>>> GetRepliesAsync(IEnumerable<int> commentIds);
		Task<List<Comment>> GetSlideTopLevelCommentsAsync(string courseId, Guid slideId);
	}
}