namespace frytech.AppleMusic.API.Models.Core;

public interface IHasAttributes<out TAttributes>
    where TAttributes : IAttributes
{
    public TAttributes Attributes { get; }
}