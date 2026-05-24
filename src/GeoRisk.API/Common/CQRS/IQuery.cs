namespace GeoRisk.API.Common.CQRS;

#pragma warning disable S2326 // Type parameter is used as a constraint link in IQueryHandler<TQuery, TResult>
public interface IQuery<TResult>;
#pragma warning restore S2326
