namespace Share.Web;

public static class Ages
{
    // Age in whole years on a given day. A birthday counts on the day itself.
    // Someone born on 29 February has their birthday on 1 March in other years (as UK law treats it),
    // which also errs towards "under 18" for the Enhanced DBS rule.
    public static int On(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        if ((today.Month, today.Day).CompareTo((dateOfBirth.Month, dateOfBirth.Day)) < 0) age--;
        return age;
    }

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}
