using HangingReferenceExample;

namespace GithubExample;

public sealed class Testing : SomeBase
{
    public override object GetData()
    {
        return new SomeObject() { Name = "Testing" };
    }
}
