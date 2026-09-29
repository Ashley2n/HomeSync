using infrastructure.Interfaces;

namespace test.setup;

public class TestCurrentHouseholdContext(Guid? householdId = null) : ICurrentHouseholdContext
{
    public Guid HouseholdId => householdId ??
                               throw new InvalidOperationException("Household not set for this test");

}