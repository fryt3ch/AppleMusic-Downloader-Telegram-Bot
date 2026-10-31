namespace frytech.AppleMusic.API.Models.Core;

public interface IHasRelationships<out TRelationships> 
    where TRelationships : IRelationships
{
    public TRelationships Relationships { get; }
}