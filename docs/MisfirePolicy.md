# MisfirePolicy

Defines the policy for handling misfired jobs (jobs that were scheduled to run but the scheduler was not running at the scheduled time).

## Policies

### FireOnceNow
- **Value**: `0`
- **Behavior**: Fire the job once immediately when the scheduler restarts after a misfire.
- **Consideration**: This can cause a "thundering herd" problem if many jobs have missed their execution windows.

### SkipToNext
- **Value**: `1`
- **Behavior**: Skip the missed execution and schedule the next execution based on the cron expression. This is the safest default for recurring jobs.

### FireAll
- **Value**: `2`
- **Behavior**: Fire all missed executions immediately when the scheduler restarts.
- **Consideration**: This can cause performance issues if many executions were missed.

## Extension Methods

### GetDescription
Returns a human-readable description of the misfire policy.

#### Usage
```csharp
using JobScheduler.Core.Constants;

// Example: Get description for FireOnceNow policy
string description = MisfirePolicy.FireOnceNow.GetDescription();
// description: "Fire the job once immediately when the scheduler restarts after a misfire."

// Example: Get description for SkipToNext policy
description = MisfirePolicy.SkipToNext.GetDescription();
// description: "Skip the missed execution and schedule the next execution based on the cron expression."

// Example: Get description for FireAll policy
description = MisfirePolicy.FireAll.GetDescription();
// description: "Fire all missed executions immediately when the scheduler restarts."
```

#### Source
```csharp
public static string GetDescription(this MisfirePolicy policy)
{
    return policy switch
    {
        MisfirePolicy.FireOnceNow => "Fire the job once immediately when the scheduler restarts after a misfire.",
        MisfirePolicy.SkipToNext => "Skip the missed execution and schedule the next execution based on the cron expression.",
        MisfirePolicy.FireAll => "Fire all missed executions immediately when the scheduler restarts.",
        _ => policy.ToString()
    };
}
```