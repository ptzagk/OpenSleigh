# OpenSleigh
[![OpenSleigh](https://circleci.com/gh/mizrael/OpenSleigh.svg?style=shield&circle-token=b7635df8feb7c79524db993c3cf962863ad28aa1)](https://app.circleci.com/pipelines/github/mizrael/OpenSleigh)

## Description
OpenSleigh is a distributed saga management library, written in C# with .NET Core 5. 
It is intended to be reliable, fast, easy to use, configurable and extensible.

## Installation
OpenSleigh can be installed from Nuget. The Core module is available here: https://www.nuget.org/packages/OpenSleigh.Core/

However, a Transport and Persistence library are necessary to properly use the library.

These are the libraries available at the moment:
- https://www.nuget.org/packages/OpenSleigh.Persistence.InMemory/
- https://www.nuget.org/packages/OpenSleigh.Persistence.Mongo/
- https://www.nuget.org/packages/OpenSleigh.Transport.RabbitMQ/

## How-to
OpenSleigh is intended to be flexible and developer friendly. It makes use of Dependency Injection for its own initialization and the setup of the dependencies.

The first step, once you have installed the [Core library](https://www.nuget.org/packages/OpenSleigh.Core/), is to add OpenSleigh to the Services collection:

```
Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) => {
                services.AddOpenSleigh(cfg =>{ ... });
    });
```

#### Adding a Saga

A Saga is a simple class inheriting from the base [`Saga<>`](https://github.com/mizrael/OpenSleigh/blob/develop/src/OpenSleigh.Core/Saga.cs) class. We also have to create an additional State class holding it's data, by inheriting from [`SagaState`](https://github.com/mizrael/OpenSleigh/blob/develop/src/OpenSleigh.Core/SagaState.cs):

```
public class MyAwesomeSagaState : SagaState{
    public MyAwesomeSagaState(Guid id) : base(id){}
}

public class MyAwesomeSaga :
    Saga<MyAwesomeSagaState>
{
    private readonly ILogger<MyAwesomeSaga> _logger;       

    public ParentSaga(ILogger<MyAwesomeSaga> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

Dependency injection can be used to reference services from Sagas.

At this point all you have to do is register and configure the Saga:
```
services.AddOpenSleigh(cfg =>{
    cfg.AddSaga<MyAwesomeSaga, MyAwesomeSagaState>()
        .UseStateFactory(msg => new MyAwesomeSagaState(msg.CorrelationId))
        .UseRabbitMQTransport(rabbitConfig)
        .UseMongoPersistence(mongoConfig);
});
```
In this example, the State for this Saga will be persisted in MongoDB, and its messages will use RabbitMQ as Transport mechanism.

#### Starting a Saga
In order to start a Saga, we need to tell OpenSleigh which message type can be used as "initiator". In order to do that, we need to add  the [`IStartedBy<>`](https://github.com/mizrael/OpenSleigh/blob/develop/src/OpenSleigh.Core/IStartedBy.cs) interface to the Saga and implement it:

```
public class MyAwesomeSaga :
    Saga<MyAwesomeSagaState>,
    IStartedBy<StartMyAwesomeSaga>
{
    public async Task HandleAsync(IMessageContext<StartMyAwesomeSaga> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"starting saga '{context.Message.CorrelationId}'...");
    }
}
```

Messages are simple POCO classes (or records), implementing the [`ICommand`](https://github.com/mizrael/OpenSleigh/blob/develop/src/OpenSleigh.Core/ICommand.cs) interface:

```
public record StartMyAwesomeSaga(Guid Id, Guid CorrelationId) : ICommand { }
```
Each message has to expose an `Id` property and a `CorrelationId`. Those are used to reconstruct the Saga State when the message is received by a subscriber. 

**IMPORTANT**: 
If a Saga is sending a message to itself (loopback), or spawning child Sagas, the `CorrelationId` has to be kept unchanged on all the messages. 
Also, make sure the `Id` and the `CorrelationId` don't match!

#### Handling messages

In order to handle more message types, it is necessary to add and implement the [`IHandleMessage<>`](https://github.com/mizrael/OpenSleigh/blob/develop/src/OpenSleigh.Core/IHandleMessage.cs) interface:

```
public class MyAwesomeSaga :
    Saga<MyAwesomeSagaState>,
    IStartedBy<StartMyAwesomeSaga>,
    IHandleMessage<MyAwesomeSagaCompleted>,
{
    // code omitted for brevity

    public async Task HandleAsync(IMessageContext<MyAwesomeSagaCompleted> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"saga '{context.Message.CorrelationId}' completed!");
    }
}
```

#### Publishing messages
A message can be published by calling the `PublishAsync()` method of `IMessageBus`. Sagas classes get an instance injected as Property:

```
public class MyAwesomeSaga :
    Saga<MyAwesomeSagaState>,
    IStartedBy<StartMyAwesomeSaga>
{
     public async Task HandleAsync(IMessageContext<StartMyAwesomeSaga> context, CancellationToken cancellationToken = default)
    {
        var message = new MyAwesomeSagaCompleted(Guid.NewGuid(), context.Message.CorrelationId);
        this.Bus.PublishAsync(message);
    }
}
```
OpenSleigh uses the [Outbox pattern](https://www.davideguida.com/improving-microservices-reliability-part-2-outbox-pattern/) to ensure messages are properly published and the Saga State is persisted.

#### Sample Application
A .NET Console application is available in the `/samples/` folder. Before running it, make sure to spin-up the required infrastructure using the provided docker-compose configuration using `docker-compose up`.

## Roadmap
- add more logging
- add Azure ServiceBus message transport
- add CosmosDB saga state persistence
