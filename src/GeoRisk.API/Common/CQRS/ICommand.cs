namespace GeoRisk.API.Common.CQRS;

#pragma warning disable S2326 // Type parameter is used as a constraint link in ICommandHandler<TCommand, TResult>
public interface ICommand<TResult>;
#pragma warning restore S2326
