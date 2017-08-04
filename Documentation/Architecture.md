# RIFF Architecture

### Introduction

RIFF Framework is a distributed processing environment with a user programming model. You can define components
which will be loaded and executed by the Framework according to its rules.

### Environment Layer

The base environment accessible to user code is a Context class.
There are different types of contextes with various amount of access.
Context provides access to configuration, logging, event/instruction queues and the data layer.

### Engine Layer

The Engine is a collection of event Reactors and instruction Processors you define.
At the heart of the system lies a message queue processing Event and Instruction messages:

* Reactors receive Event callbacks and can generate an Instruction targeting a specific Processor with an optional parameter
* Processors receive callbacks when an Instruction targets them with the optional parameter
* Processors can generate further instruction and events to the queue by using its Context object

### Data Layer

Catalog is a key-value store available via IRFCatalog interface. An update of a Catalog entry
generates an RFCatalogUpdateEvent for which you can set up reactors.

### Graph Layer

Graph Layer is an abstraction on top of the Engine Layer where every Instruction
is attributed with a parameter of type RFGraphInstance (which is a date and a string).

You can define GraphProcessors by deriving from RFGraphProcessor&lt;D&gt; operating over a Domain class D,
whose properties can be mapped to GraphInstance-parametrized keys in the Catalog. The Framework
will automatically trigger processing of relevant GraphProcessors whenever their mapped inputs in are updated
in the Catalog, and that processing will be parametrized by respective update's GraphInstance.

There are three types of IO mappings:
* Input - whenever updated the GraphProcessor will be fired (except when the Input is Dateless)
* State (Input and Output) - an update won't trigger processing
* Output
which are specified as attributes on Domain properties. When mapping domain properties you
also have to specify Date behaviour for the mapped Catalog item:
* Dateless - loaded Catalog item will have null date, this is used for static data
* Exact - loaded Catalog item will have same date as the ValueDate of the calculation
* Latest - loaded Catalog will have latest available date relative to the ValueDate
* Previous - loaded Catalog will have the latest available date, but earlier than ValueDate
* Range - used to load inputs across a date range

| IO Behaviour | Date Behaviour   | Triggers? | Before | After     |
| ------------ | ---------------- | --------- | ------ | --------- |
| Input        | all ex. Dateless | Yes       | Loaded | Discarded |
| Input        | Dateless         | No        | Loaded | Discarded |
| State        | all              | No        | Loaded | Saved     |
| Output       | Exact            | -         | null   | Saved     |

Graph Framework is forward-looking in terms of calculation dependency and GraphProcessors will be
run in correct depencency order. In order to ensure lack of side-effects, GraphProcessors don't have
access to Context object (they cannot generate events or load/save arbitrary Catalog entries)
and can only operate within their Domain object.

### Additional Components

#### Interval and Scheduling

RFIntervalEvent is an interval event fired in predefined time intervals (default is every 60 seconds).
RFSchedulerProcessor can be used to react to that event and generate Instructions
within a specific time range. For setting up scheduled tasks a convenience AddScheduledTask method
is provided on the EngineConfiguration object.
