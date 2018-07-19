# ServiceFabricQueuedServices.Services

Helper library for setting up `ServiceInstanceListener`s for queued calls using Azure Service Bus ***queues***.
This functionality *WILL NOT* work with Azure Service Bus ***topics + subscriptions***. All that's required is a 
`NetMessagingBinding` of your own configuration, and an Azure Service Bus connection string with `listen` privileges.

## Installation

The Nuget package can be downloaded [here](https://www.nuget.org/packages/ServiceFabricQueuedServices.Services/), or can be installed within the Visual Studio Package Manager Console with the command:
```
PM> Install-Package ServiceFabricQueuedServices.Services -Prerelease
```

## Usage

To use this package, you'll need to execute the following steps:
  1. Create a setting in your `Settings.xml` file for the connection string with `listen` permissions:
```
<?xml version="1.0" encoding="utf-8" ?>
<Settings xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.microsoft.com/2011/01/fabric">
	<!-- Add your custom configuration sections and parameters here -->
	<Section Name="ServiceBus">
		<Parameter Name="ListenerConnectionString" Value="Endpoint=sb://mysbnamespace.servicebus.windows.net/;SharedAccessKeyName=ListenAccessKey;SharedAccessKey=abc/5yj8/yMm+123j456gbbbb4bbbbbZtsvWjHLp+hM=" />
	</Section>
</Settings>
``` 
  2. In your `StatelessService` service implementation, which also implements your WCF interface decorated with `[ServiceContract]` and `[OperationContract]` attributes, override the `CreateServiceInstanceListeners()` method to register your `ServiceInstanceListener` for the Azure Service Bus queue:
```
	/// <summary>
	/// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
	/// </summary>
	/// <returns>A collection of listeners.</returns>
	protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
	{
		FabricTelemetryInitializerExtension.SetServiceCallContext(this.Context);

		yield return QueuedServiceInstanceListenerUtility.CreateQueuedServiceBusListener<INotificationEvents>(
			wcfServiceObject: this, 
			netMessagingBinding: new NetMessagingBinding(), 
			listenConnectionStringParameterName: "ListenerConnectionString"
		);
	}
```
  3. Implement your WCF service methods in your `StatelessService` implementation and you're done:
```
	/// <inheritdoc />
	public Task DoMyActionAsync(MyComplexType myType)
	{
		throw new NotImplementedException();
	}
```