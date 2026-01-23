using Microsoft.Extensions.Logging;
using ProjectManagement.Core.Entities;
using ProjectManagement.Core.Enums;
using ProjectManagement.Core.Exceptions;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Application.Services;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Business logic service for WorkItem operations
/// </summary>
public class WorkItemService : IWorkItemService
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStateManager _stateManager;
    private readonly ILogger<WorkItemService> _logger;

    public WorkItemService(
        IWorkItemRepository workItemRepository,
        IUnitOfWork unitOfWork,
        IStateManager stateManager,
        ILogger<WorkItemService> logger)
    {
        _workItemRepository = workItemRepository;
        _unitOfWork = unitOfWork;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task<WorkItem> CreateAsync(
        WorkItemType type,
        string title,
        string description,
        string? acceptanceCriteria,
        int priority,
        CancellationToken cancellationToken = default)
    {
        // Validate priority range
        if (priority < 1 || priority > 9)
        {
            throw new ArgumentException("Priority must be between 1 and 9", nameof(priority));
        }

        var workItem = new WorkItem
        {
            Type = type,
            Title = title,
            Description = description,
            AcceptanceCriteria = acceptanceCriteria,
            Priority = priority,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _workItemRepository.AddAsync(workItem, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Created WorkItem {Id} of type {Type}", workItem.Id, workItem.Type);
        return workItem;
    }

    public async Task<WorkItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _workItemRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<WorkItem?> GetWithDeveloperStoriesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _workItemRepository.GetWithDeveloperStoriesAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<WorkItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _workItemRepository.GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkItem>> GetByStatusAsync(
        WorkItemStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _workItemRepository.GetByStatusAsync(status, cancellationToken);
    }

    public async Task UpdateStatusAsync(
        int id,
        WorkItemStatus newStatus,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var workItem = await _workItemRepository.GetByIdAsync(id, cancellationToken);
        if (workItem == null)
        {
            throw new EntityNotFoundException(typeof(WorkItem), id);
        }

        // Validate state transition
        if (!_stateManager.CanTransition(workItem.Status, newStatus))
        {
            throw new InvalidStateTransitionException(workItem.Status, newStatus, typeof(WorkItem));
        }

        workItem.UpdateStatus(newStatus, errorMessage);
        await _workItemRepository.UpdateAsync(workItem, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Updated WorkItem {Id} status from {OldStatus} to {NewStatus}",
            id, workItem.Status, newStatus);
    }

    public async Task<bool> HasInProgressUserStoryAsync(CancellationToken cancellationToken = default)
    {
        var inProgressItem = await _workItemRepository.GetInProgressAsync(cancellationToken);
        return inProgressItem != null && inProgressItem.Type == WorkItemType.UserStory;
    }
}
