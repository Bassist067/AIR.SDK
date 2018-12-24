# AIR.SDK
---------------------------------------

{Description}

See the [change log](CHANGELOG.md) for changes and road map.

This solution is for people who work with the AWS .NET SDK. These are our helper libraries to simplify working with AWS SQS, AWS S3 and AWS SWF.

## Features

- Queue Manager
- Storage Manager
- Workflow Manager

### Queue Manager
Helps with the .NET SQS library. Alllows subscribing to the queues.

### Storage Manager
Helps with the .NET S3 library. Just a standard wrapper.

#### Workflow Manager
Allows to ease the pain with using .NET SWF library. Provides abstraction level above the SWF entities, so you will be able to operate with the workflows and activities. Please note this is a port from .NET 4.5, so some tests are still broken.

## Contribute
Check out the [contribution guidelines](CONTRIBUTING.md)
if you want to contribute to this project.

## License
[Apache 2.0](LICENSE)
