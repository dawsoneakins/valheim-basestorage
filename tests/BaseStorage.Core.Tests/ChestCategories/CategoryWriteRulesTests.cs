namespace BaseStorage.Core.Tests.ChestCategories;

using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.ChestCategories.ChestCategoryFixtures;

/// <summary>GW-U-1208..1211 (AC-3): the pure write rules, the deadline, the notice keys, and the two value types (contract 2.3, 2.4, 5.3).</summary>
public sealed class CategoryWriteRulesTests
{
    private static readonly ContainerId Chest42 = new ContainerId(3141592653L, 42u);

    // GW-U-1208: the action per reservation state.
    [Theory]
    [InlineData(ReservationState.Requesting, PendingWriteAction.Wait)]
    [InlineData(ReservationState.Granted, PendingWriteAction.Write)]
    [InlineData(ReservationState.Writing, PendingWriteAction.Fail)]
    [InlineData(ReservationState.Releasing, PendingWriteAction.Fail)]
    [InlineData(ReservationState.Completed, PendingWriteAction.Fail)]
    [InlineData(ReservationState.Failed, PendingWriteAction.Fail)]
    [InlineData((ReservationState)99, PendingWriteAction.Fail)]
    public void ActionFollowsTheReservationState(ReservationState state, PendingWriteAction expected)
    {
        Assert.Equal(expected, CategoryWriteRules.ActionFor(state));
    }

    // GW-U-1208: no reservation is a failure.
    [Fact]
    public void NoReservationFails()
    {
        Assert.Equal(PendingWriteAction.Fail, CategoryWriteRules.ActionFor(null));
        Assert.Equal(CategoryWriteOutcome.Unavailable, CategoryWriteRules.FailureOutcome(null, ReservationFailure.Denied));
    }

    // GW-U-1209: the failure-to-outcome map and the notice and reason keys.
    [Theory]
    [InlineData(ReservationFailure.Denied, CategoryWriteOutcome.Denied, "category.refusal.denied")]
    [InlineData(ReservationFailure.InUseByOther, CategoryWriteOutcome.InUse, "category.refusal.in-use")]
    [InlineData(ReservationFailure.Timeout, CategoryWriteOutcome.Timeout, "category.refusal.timeout")]
    [InlineData(ReservationFailure.OwnerLost, CategoryWriteOutcome.Unavailable, "category.refusal.unavailable")]
    [InlineData(ReservationFailure.RequesterDisconnected, CategoryWriteOutcome.Unavailable, "category.refusal.unavailable")]
    [InlineData(ReservationFailure.Cancelled, CategoryWriteOutcome.Unavailable, "category.refusal.unavailable")]
    [InlineData(ReservationFailure.None, CategoryWriteOutcome.Unavailable, "category.refusal.unavailable")]
    [InlineData((ReservationFailure)42, CategoryWriteOutcome.Unavailable, "category.refusal.unavailable")]
    public void FailuresMapToOutcomesAndReasonKeys(ReservationFailure failure, CategoryWriteOutcome outcome, string reasonKey)
    {
        Assert.Equal(outcome, CategoryWriteRules.OutcomeFor(failure));
        Assert.Equal(outcome, CategoryWriteRules.FailureOutcome(ReservationState.Failed, failure));
        Assert.Equal(CategoryWriteOutcome.Unavailable, CategoryWriteRules.FailureOutcome(ReservationState.Completed, failure));
        Assert.Equal(reasonKey, CategoryWriteRules.ReasonKey(outcome));
        Assert.Equal(StringKeys.MessageCategoryRefused, CategoryWriteRules.NoticeKey(outcome));
        Assert.Equal(CategoryWriteStatus.Refused, CategoryWriteRules.StatusOf(outcome));
        Assert.True(StringTable.English.Contains(reasonKey));
    }

    // GW-U-1209: Applied has the set notice, no reason and the Applied status; an undefined outcome falls back to Unavailable's reason.
    [Fact]
    public void AppliedUsesTheSetNoticeAndHasNoReason()
    {
        Assert.Equal(StringKeys.MessageCategorySet, CategoryWriteRules.NoticeKey(CategoryWriteOutcome.Applied));
        Assert.Null(CategoryWriteRules.ReasonKey(CategoryWriteOutcome.Applied));
        Assert.Equal(CategoryWriteStatus.Applied, CategoryWriteRules.StatusOf(CategoryWriteOutcome.Applied));
        Assert.Equal(StringKeys.CategoryRefusalUnavailable, CategoryWriteRules.ReasonKey((CategoryWriteOutcome)77));
        Assert.Equal(StringKeys.MessageCategoryRefused, CategoryWriteRules.NoticeKey((CategoryWriteOutcome)77));
    }

    // GW-U-1209: the refusal notice renders with each reason, in English.
    [Theory]
    [InlineData(CategoryWriteOutcome.Denied, "Chest category not changed: no access to this chest")]
    [InlineData(CategoryWriteOutcome.InUse, "Chest category not changed: in use by Erik")]
    [InlineData(CategoryWriteOutcome.Timeout, "Chest category not changed: no answer from the chest owner")]
    [InlineData(CategoryWriteOutcome.Unavailable, "Chest category not changed: the chest is not available")]
    public void RefusalNoticesRenderInEnglish(CategoryWriteOutcome outcome, string expected)
    {
        StringTable strings = StringTable.English;
        string reason = strings.Format(CategoryWriteRules.ReasonKey(outcome)!, "Erik");

        Assert.Equal(expected, strings.Format(CategoryWriteRules.NoticeKey(outcome), reason));
    }

    // GW-U-1210: the deadline is twice the timeout after the start, with values below 1 read as 1, never throwing.
    [Fact]
    public void DeadlineIsTwiceTheTimeoutAfterTheStart()
    {
        TimeSpan start = TimeSpan.FromSeconds(3600);

        Assert.Equal(TimeSpan.FromSeconds(3603), CategoryWriteRules.Deadline(start, 1500));
        Assert.Equal(start + TimeSpan.FromMilliseconds(2), CategoryWriteRules.Deadline(start, 0));
        Assert.Equal(start + TimeSpan.FromMilliseconds(2), CategoryWriteRules.Deadline(start, -7));
        Assert.True(CategoryWriteRules.Deadline(start, int.MaxValue) > start);
    }

    // GW-U-1211: PendingCategoryWrite validates, exposes its members, expires at the deadline or when the clock goes back, and is a value.
    [Fact]
    public void PendingWriteIsAValidatedValue()
    {
        TimeSpan start = TimeSpan.FromSeconds(3600);
        TimeSpan deadline = TimeSpan.FromSeconds(3603);
        var write = new PendingCategoryWrite(Chest42, Cat("Wood"), start, deadline);

        Assert.Equal(Chest42, write.Chest);
        Assert.Equal(Cat("Wood"), write.Desired);
        Assert.Equal(start, write.StartedAt);
        Assert.Equal(deadline, write.Deadline);
        Assert.False(write.IsExpired(start));
        Assert.False(write.IsExpired(TimeSpan.FromTicks(deadline.Ticks - 1)));
        Assert.True(write.IsExpired(deadline));
        Assert.True(write.IsExpired(TimeSpan.FromSeconds(3599)));

        Assert.Equal(write, new PendingCategoryWrite(Chest42, Cat("Wood"), start, deadline));
        Assert.Equal(write.GetHashCode(), new PendingCategoryWrite(Chest42, Cat("Wood"), start, deadline).GetHashCode());
        Assert.NotEqual(write, new PendingCategoryWrite(Chest42, null, start, deadline));
        Assert.True(new PendingCategoryWrite(Chest42, null, start, start) == new PendingCategoryWrite(Chest42, null, start, start));
        Assert.True((PendingCategoryWrite?)null == (PendingCategoryWrite?)null);
        Assert.False(write == null);
        Assert.False(write.Equals("x"));

        Assert.Equal("chest", Assert.Throws<ArgumentException>(() => new PendingCategoryWrite(default, null, start, deadline)).ParamName);
        Assert.Equal("deadline", Assert.Throws<ArgumentOutOfRangeException>(() => new PendingCategoryWrite(Chest42, null, deadline, start)).ParamName);
    }

    // GW-U-1211: CategoryWriteResult validates, exposes its members and is a value with an ordinal holder label.
    [Fact]
    public void WriteResultIsAValidatedValue()
    {
        var result = new CategoryWriteResult(Chest42, null, CategoryWriteOutcome.InUse, "Erik");

        Assert.Equal(Chest42, result.Chest);
        Assert.Null(result.Desired);
        Assert.Equal(CategoryWriteOutcome.InUse, result.Outcome);
        Assert.Equal("Erik", result.HolderLabel);
        Assert.False(result.Succeeded);
        Assert.True(new CategoryWriteResult(Chest42, CategoryAssignment.Keep, CategoryWriteOutcome.Applied, null).Succeeded);

        Assert.Equal(result, new CategoryWriteResult(Chest42, null, CategoryWriteOutcome.InUse, "Erik"));
        Assert.Equal(result.GetHashCode(), new CategoryWriteResult(Chest42, null, CategoryWriteOutcome.InUse, "Erik").GetHashCode());
        Assert.NotEqual(result, new CategoryWriteResult(Chest42, null, CategoryWriteOutcome.InUse, "erik"));
        Assert.NotEqual(result, new CategoryWriteResult(Chest42, null, CategoryWriteOutcome.Denied, "Erik"));
        Assert.True(result != new CategoryWriteResult(Chest42, Cat("Wood"), CategoryWriteOutcome.InUse, "Erik"));
        Assert.True((CategoryWriteResult?)null == (CategoryWriteResult?)null);
        Assert.False(result.Equals(null));

        Assert.Equal("chest", Assert.Throws<ArgumentException>(() => new CategoryWriteResult(default, null, CategoryWriteOutcome.Applied, null)).ParamName);
    }

    // GW-U-1211: the three enums have the contracted names and values and an int underlying type.
    [Fact]
    public void EnumsHaveTheContractedValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(CategoryWriteOutcome)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(CategoryWriteStatus)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(PendingWriteAction)));
        Assert.Equal(new[] { "Applied=0", "Denied=1", "InUse=2", "Timeout=3", "Unavailable=4" }, Members<CategoryWriteOutcome>());
        Assert.Equal(new[] { "Applied=0", "Pending=1", "Unchanged=2", "Refused=3" }, Members<CategoryWriteStatus>());
        Assert.Equal(new[] { "Wait=0", "Write=1", "Fail=2" }, Members<PendingWriteAction>());
    }

    private static string[] Members<TEnum>()
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(v => v.ToString() + "=" + Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
}
