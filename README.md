# ServiceFabricQueuedServices.Services

Helper library for setting up `ServiceInstanceListener`s for queued calls using Azure Service Bus ***queues***.
This functionality *WILL NOT* work with Azure Service Bus ***topics + subscriptions***. All that's required is a 
`NetMessagingBinding` of your own configuration, and an Azure Service Bus connection string with `listen` privileges.

For example:
```
TODO
```