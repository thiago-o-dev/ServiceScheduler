namespace SharedKernel.Abstractions;

public abstract class LifeCycleEntity : BaseEntity
{
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
    public bool IsActive { get; protected set; } = true;

    public virtual void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
