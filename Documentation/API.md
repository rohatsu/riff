# RIFF API

## Data Structures

You can define your own data structures. They will be serialized using DataContractSerializer, and you can choose to either
decorate them with DataContract or not (remember about the parameterless constructor) depending on your needs. RIFF
will cache serializers for all classes with DataContract or RFCacheSerializer attribute at startup.
`[OnSerializing`] and `[OnDeserialized]` attributes are also supported.

There are certain data structures you can reuse to use built-in functionality.

##### RFDataSet

* `class RFDataSet<T> where T : IRFDataRow`

Serves as base for tabular data of unordered datasets. The order of rows is not guaranteed to be persisted but
also if your code generates rows in parallel, changes in row order will not be considered a data update.

You should not derive from this class but instead define your own row type (by subclassing RFDataRow) and use it
as the generic parameter.

##### RFMappingDataSet

* `class RFMappingDataSet<K, R> : RFDataSet<R> where R : RFMappingDataRow<K> where K : RFMappingKey`

Serves as base for tabular data with a unique lookup key, operating in a similar manner to Dictionary and exposing
a range of functions to get/create entries. To use, derive from RFMappingKey and RFMappingDataRow<K> and use these
as generic parameters.

#### RFDataMatrix

* `class RFDataMatrix<K1, K2, C> : IRFDataSet where K1 : IComparable where K2 : IComparable`

Represents a two-dimensional matrix optimized for sparse data (only cells containing values are kept), fast lookup and
efficient serialization.

##### RFRawReport

* `class RFRawReport`

Represents unstructured input report composed of sections (`RFRawReportSection`), which are in turn composed of tabular
data. This is usually an output from RIFF's frame

## Processing

### Engine Layer

The engine layer allows you to develop processors that can:
* take execution parameters
* directly access the Catalog without restrictions
* fire events and issue instructions to trigger other processors

and are automatically run by the framework in response to triggers and events.

This layer is best suited when a graph approach is not possible, for example at the edges of processing 
when it's not known what the exact inputs' parameters will be (e.g. importing an external
report - value date can only be determined after it's been inspected).

Processors will be run in parallel if they have different parameters. When parameters are the same, one extra
instance will be queued but no more (in case of long-running scheduled tasks they won't pile up).

##### RFEngineProcessor

* `class RFEngineProcessor<P> where P : RFEngineProcessorParam`
* `class RFEngineProcessorWithConfig<P, C> : RFEngineProcessor<P> where P : RFEngineProcessorParam where C : IRFEngineProcessorConfig`
* `class RFGenericSingleInstanceProcessor : RFEngineProcessor<RFEngineProcessorParam>`

To develop an engine processor derive from one of these classes. Subclass `RFEngineProcessorParam` to define your
own execution parameter or use one of the built-in parameters:
* `RFEngineProcessorKeyParam` - contains `RFCatalogKey` as parameter, automatically provided from Catalog update triggers
* `RFEngineProcessorGraphInstanceParam` - contains `RFGraphInstance` (string and date) as parameter, useful for processors running at the edge of a graph or as Scheduled Tasks
* `RFEngineProcessorIntervalParam` - constains `RFInterval` as parameter which is the internal heartbeat (default 60 seconds)

`IRFEngineProcessorConfig` is an optional parameter defined at startup and passed during processor initialization.
Please note `RFEngineProcessors` are not reused and have single-call, single-thread lifetime.

To add processing logic implement the `Process()` abstract member, returning a simple 'RFProcessingResult' object
with execution statistics. Use `Context`, `InstanceParams` and `KeyDomain` members to access the Catalog, execution
parameters and create keys.

#### Defining Engines

To define an engine, write a class implementing `IRFEngineBuilder` and its member `BuildEngine` returning an
`RFEngineDefinition` object which you can create using its `Create` static method.

To add individual processors, use `AddProcess<P>` method exposed by `RFEngineDefinition`, where you need to provide:
* a name which needs to be unique
* a function creating the processor (this will be run before each individual processor execution)
* an optional function extracting the execution parameter from an instruction, necessary only when using custom parameter types

Once a processor is added it needs to be set up for execution as either tasks:
* `AddTriggeredTask` where the trigger is a Catalog key update, please note key matching is based on key root only
so keys parametrized by `RFGraphInstance` will also trigger the calculation, and either the whole key or just
`RFGraphInstance` portion will be passed as execution the parameter (depending on which is defined on processor)
* `AddScheduledTask` where the trigger is based on a Schedule
* `AddChainedTask` where the trigger is the completion of another task

or using raw framework methods for more control:
* `AddCatalogUpdateTrigger<K>` which allows you to define an evaluator function for a specific key type that
will determine if the processor should be run or not; use this to override default root-based matching and
react to a whole key type, only an individual key or another filter based on key contents
* `AddIntervalTrigger` which will run the processor on every heartbeat (default 60 seconds)

### Graph Layer

Graph layer allows you to develop processors that:
* run with a well defined IO (rules for fetching inputs/outputs/state determined at startup)
* run parametrised by a key consiting of: a string (independent planes) and a date (continuity-aware: earlier data affects later data)

and are automatically sorted by dependency order by the framework and run whenever their inputs have changed.

Through strict IO rules the graph layer makes it easy to add additional steps into a data processing framework with
automatically determined calculation order and automatic triggers. The graph can also be represented visually using
the Graph Browser function in the UI.

Graph processors will be run in parallel when there is no interdependency between them and queued
appropriately when there is to reduce the number of executions.

##### RFGraphProcessor

* `class RFGraphProcessor<D> where D : RFGraphProcessorDomain`
* `class RFGraphProcessorWithConfig<D, C> : RFGraphProcessor<D> where D : RFGraphProcessorDomain where C : IRFGraphProcessorConfig`

To implement business logic derive from one of the above classes and provide a subclass of `RFGraphProcessorDomain`
which defines the IO of the processor. Add public auto members in your domain class to IO, each property must be decorated
by `RFIOBehaviour` attribute determining whether it's an Input, Output or State, and also in case of inputs
whether it's mandatory or optional.

To add processing logic implement the `Process()` abstract member, which will be called by the framework with your domain
object populated from the Catalog. Populate `RFOutput` members and change `RFState` objects of that domain object
(changes to `RFInput` members will be discarded).

##### RFGraphInstance

All graph executions are run within a `RFGraphInstance` context. This is a class with two members:
* `Name` is the name of the plane with the default being `default`; planes are independent of each other
* `ValueDate` is a date and the framework provides specialised logic for chronological interdependent data (see below)

#### Defining Graphs

To define a graph first create an `RFGraphDefinition` object using `CreateGraph` method of your `RFEngineDefinition`.
Then use its `AddProcess<D>` method to create processors where you need to provide:
* a name (unique within the graph)
* a function that creates the processor

Once you have `RFGraphProcessDefinition<D>` object for your processor use its `Map<D>` method to define IO:
* provide a function that chooses the property to map from the Domain object, this is for type-safety
* provide an `RFCatalogKey` that will be mapped to that property (the value with the same `RFGraphInstance`)
* define `RFDateBehaviour` which determines which exact instance of the mapped key should be provided:
    * `Dateless` - the key with no instance will be provided
    * `Exact` - the key with the same `RFGraphInstance` as the processor's execution context will be provided
    * `Latest` - the key with the latest `ValueDate` in its `RFGraphInstance` less or equal execution context's `ValueDate` will be provided
    * `Previous` - the key with the latest `ValueDate` in its `RFGraphInstance` less execution context's `ValueDate` will be provided
    * `Range` - the set of keys within a specific range will be returned, see Ranged IO below section for details

The combination of `RFIOBehaviour` and `RFDateBehaviour` determines when the framework will trigger calculation on a key's update:

| IO Behaviour | Date Behaviour   | Triggers? | Before | After     |
| ------------ | ---------------- | --------- | ------ | --------- |
| Input        | all ex. Dateless | Yes       | Loaded | Discarded |
| Input        | Dateless         | No        | Loaded | Discarded |
| State        | all              | No        | Loaded | Saved     |
| Output       | Exact            | -         | null   | Saved     |

The rules of the thumb are:
* `Exact` is the most common case as it defines data that is specific only to the calculation date
* `Latest` is normally used for data that gets periodically updated over time but needs to be kept historically (i.e. name change)
* `Dateless` should normally hold State or static data that shouldn't trigger recalculation (for example large incremental datasets)
* `Previous` is a helper useful for change on day

One alternative method is to use `AddScheduledTask<D>` method on `RFGraphDefinition` object to run the processor
as a Scheduled Task, when for example it has no inputs.

##### Ranged IO

In order to receive a set of keys as inputs to a graph processor you need to:
* define the mapped property as `RFRangeInput<T>` where `T` is type of the individual object
* use `MapRange` method and provide two extra functions to it:
    * `RangeRequestFunc` that for a given `RFGraphInstance` about to be executed, returns the list of `ValueDates` for which keys are requested
    * `RangeUpdateFunc` which is its inverse that for a given `ValueDate` of an updated range key returns the `RFGraphInstance` that should be triggered for processing; please note this means range inputs should not overlap (i.e. if you look at week's range of inputs only run you processor once a week)

## Patterns

## Processor Library

#### RFDataSetBuilderProcessor

Scaffolding for a simple processor taking in `RFRawReport` and converting it into an `IRFDataSet' type.

#### RFDataSetSinkSQL

Maps an `IRFDataSet` into an SQL table to make datasets acessible to users via an SQL database.

#### RFSchedulerProcessor

While normally you would use `AddScheduledTask` to set up processing on schedule, you can
also access the raw processor for custom cases.

#### RFFileWatcherProcessor

Automatically downloads files from SFTP or local sites.

#### RFReportParserProcessor

Converts raw files (.csv, .xls, .xlsx) to `RFRawReportFormat` based on provided configuration.
It's possible to plug in custom format loaders as well (for .pdf or .xml for example).

#### RFSQLReportRunner

Quick way to run an SQL query and output in .csv format.

#### RFActivityRunnerProcessor

Allows encapsulation of `RFActivity` within a processor.

#### RFEmail

Encapsulates e-mail generation and sending functionality using Razor views as templates.

#### RFEntryNotification

Sends an e-mail whenever its input has been updated.

## Logging

#### RFProcessLog

Helper class encapsulating access to logging and user notification.

# Interfaces

RIFF.Interfaces contains interfaces to common file formats and connectivity protocols.

## Programming Guidelines

* Do not leave additional threads running in processors after returning from the Process method
* Do not rely on `DateTime.Today` instead look at `Context.Today()` (engine) or `ValueDate()` (graph)
* Serialization is critical, ensure your custom objects are correctly serialized and deserialized -
you can access `RFXMLSerializer` class to help you check that
