/****** Object:  Schema [RIFF]    Script Date: 04/08/2017 12:40:09 ******/
CREATE SCHEMA [RIFF]
GO
/****** Object:  UserDefinedFunction [RIFF].[StripInstance]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create function [RIFF].[StripInstance]( @input varchar(2048) )
returns varchar(2048)
as
begin
	declare @idx1 int
	select @idx1 = charindex('GraphInstance>', @input)
	if @idx1 is not null
	begin
		declare @idx2 int		
		select @idx2 = charindex('GraphInstance>', @input, @idx1 + 1)
		if @idx2 is not null
		begin
			return substring(@input, 0, @idx1) + substring(@input, @idx2, 99999)
		end
	end
	return @input
end

GO
/****** Object:  Table [RIFF].[UserRole]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[UserRole](
	[UserRoleID] [int] IDENTITY(1,1) NOT NULL,
	[RoleName] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[UserRoleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [RIFF].[UserRoleMembership]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[UserRoleMembership](
	[UserRoleMembershipID] [int] IDENTITY(1,1) NOT NULL,
	[UserRoleID] [int] NOT NULL,
	[UserName] [nvarchar](100) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserRoleMembershipID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [RIFF].[UserRolePermission]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[UserRolePermission](
	[UserRolePermissionID] [int] IDENTITY(1,1) NOT NULL,
	[UserRoleID] [int] NOT NULL,
	[Area] [nvarchar](100) NULL,
	[Controller] [nvarchar](100) NULL,
	[Permission] [nvarchar](100) NULL,
	[IsAllowed] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserRolePermissionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  View [RIFF].[UserPermissionView]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create view [RIFF].[UserPermissionView] as
	select [RoleName], [UserName], [Area], [Controller], [Permission], [IsAllowed], r.[UserRoleID] from 
	RIFF.UserRole r
	left outer join RIFF.UserRolePermission rp on rp.UserRoleID = r.UserRoleID
	left outer join RIFF.UserRoleMembership rm on rm.UserRoleID = r.UserRoleID

GO
/****** Object:  Table [RIFF].[CatalogEntry]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[CatalogEntry](
	[CatalogEntryID] [bigint] IDENTITY(1,1) NOT NULL,
	[CatalogKeyID] [bigint] NOT NULL,
	[Version] [int] NOT NULL,
	[Metadata] [xml] NULL,
	[IsValid] [bit] NOT NULL,
	[UpdateTime] [datetimeoffset](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CatalogEntryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY],
 CONSTRAINT [UQ_EntryVersion] UNIQUE NONCLUSTERED 
(
	[CatalogKeyID] ASC,
	[Version] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [RIFF].[CatalogKey]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[CatalogKey](
	[CatalogKeyID] [bigint] IDENTITY(1,1) NOT NULL,
	[KeyType] [varchar](100) NOT NULL,
	[SerializedKey] [xml] NULL,
	[KeyHash] [int] NOT NULL,
	[RootHash] [int] NOT NULL,
	[FriendlyString] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[CatalogKeyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [RIFF].[CatalogDocument]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[CatalogDocument](
	[CatalogEntryID] [bigint] NOT NULL,
	[ContentType] [varchar](200) NOT NULL,
	[BinaryContent] [varbinary](max) NULL,
 CONSTRAINT [PK_Document] PRIMARY KEY CLUSTERED 
(
	[CatalogEntryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  View [RIFF].[DocumentView]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [RIFF].[DocumentView] WITH SCHEMABINDING AS
	SELECT [CatalogKey].[CatalogKeyID], [CatalogKey].[KeyType], [CatalogKey].[SerializedKey], [Version], [Metadata], [IsValid], [UpdateTime], [CatalogDocument].BinaryContent, [CatalogDocument].ContentType, [CatalogDocument].CatalogEntryID
	FROM [RIFF].[CatalogKey] JOIN [RIFF].[CatalogEntry] ON [CatalogKey].[CatalogKeyID] = [CatalogEntry].[CatalogKeyID]
	JOIN [RIFF].[CatalogDocument] ON [CatalogDocument].[CatalogEntryID] = [CatalogEntry].[CatalogEntryID]


GO
/****** Object:  View [RIFF].[DocumentLatestView]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [RIFF].[DocumentLatestView] WITH SCHEMABINDING AS
	SELECT dv.CatalogKeyID, dv.KeyType, dv.SerializedKey, dv.Version, dv.Metadata,
	 dv.IsValid, dv.UpdateTime, dv.CatalogEntryID, dv.ContentType, dv.BinaryContent FROM [RIFF].[DocumentView] dv
	INNER JOIN (
		SELECT CatalogKeyID, Max(Version) as Version FROM [RIFF].[CatalogEntry] GROUP by CatalogKeyID ) l
	on dv.CatalogKeyID = l.CatalogKeyID and dv.Version = l.Version


GO
/****** Object:  View [RIFF].[KeysView]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [RIFF].[KeysView] AS
WITH
XMLNAMESPACES (N'RIFF.Core' as c, 'RIFF.Framework' as f)
SELECT [CatalogKeyID], [KeyType], [ContentType], [Metadata], [Version], [UpdateTime], CAST([SerializedKey] AS VARCHAR(2048)) as [SerializedKey],
[IsValid], DATALENGTH([BinaryContent]) as DataSize,
[SerializedKey].value('(//c:GraphInstance/c:Name/text())[1]', 'varchar(100)') as GraphInstanceName,
[SerializedKey].value('(//c:GraphInstance/c:ValueDate/c:YMD/text())[1]', 'int') as GraphInstanceDate
FROM [RIFF].[DocumentLatestView] WITH(NOLOCK)

GO
/****** Object:  Table [RIFF].[CatalogBlob]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[CatalogBlob](
	[CatalogEntryID] [bigint] NOT NULL,
	[FileName] [nvarchar](300) NULL,
	[ContentType] [varchar](50) NOT NULL,
	[Data] [varbinary](max) NULL,
 CONSTRAINT [PK_Blob] PRIMARY KEY CLUSTERED 
(
	[CatalogEntryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  View [RIFF].[BlobView]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- drop view RIFF.[BlobView]
CREATE VIEW [RIFF].[BlobView] AS
	SELECT [CatalogKey].[CatalogKeyID], [CatalogKey].[KeyType], [CatalogKey].[SerializedKey], [Version], [Metadata], [IsValid], [UpdateTime], [CatalogBlob].*
	FROM [RIFF].[CatalogKey] JOIN [RIFF].[CatalogEntry] ON [CatalogKey].[CatalogKeyID] = [CatalogEntry].[CatalogKeyID]
	JOIN [RIFF].[CatalogBlob] ON [CatalogBlob].[CatalogEntryID] = [CatalogEntry].[CatalogEntryID]

GO
/****** Object:  Table [RIFF].[CatalogStructure]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[CatalogStructure](
	[CatalogEntryID] [bigint] NOT NULL,
	[TableName] [varchar](200) NOT NULL,
 CONSTRAINT [PK_Structure] PRIMARY KEY CLUSTERED 
(
	[CatalogEntryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  View [RIFF].[StructureView]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- drop view RIFF.[StructureView]
CREATE VIEW [RIFF].[StructureView] AS
	SELECT [CatalogKey].[CatalogKeyID], [CatalogKey].[KeyType], [CatalogKey].[SerializedKey], [Version], [Metadata], [IsValid], [UpdateTime], [CatalogStructure].*
	FROM [RIFF].[CatalogKey] JOIN [RIFF].[CatalogEntry] ON [CatalogKey].[CatalogKeyID] = [CatalogEntry].[CatalogKeyID]
	JOIN [RIFF].[CatalogStructure] ON [CatalogStructure].[CatalogEntryID] = [CatalogEntry].[CatalogEntryID]

GO
/****** Object:  Table [RIFF].[UserConfigKey]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[UserConfigKey](
	[UserConfigKeyID] [int] IDENTITY(1,1) NOT NULL,
	[Section] [nvarchar](100) NOT NULL,
	[Item] [nvarchar](100) NOT NULL,
	[Key] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[UserConfigKeyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [RIFF].[UserConfigValue]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[UserConfigValue](
	[UserConfigValueID] [int] IDENTITY(1,1) NOT NULL,
	[UserConfigKeyID] [int] NOT NULL,
	[Environment] [varchar](20) NOT NULL,
	[Value] [nvarchar](2048) NULL,
	[Version] [int] NOT NULL,
	[UpdateTime] [datetimeoffset](7) NOT NULL,
	[UpdateUser] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[UserConfigValueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  View [RIFF].[UserConfigView]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- drop view [RIFF].[UserConfigView]
CREATE VIEW [RIFF].[UserConfigView] AS
	SELECT
	uk.[UserConfigKeyID], uk.[Section], uk.[Item], uk.[Key], uk.[Description],
	uv.[UserConfigValueID],
	uv.[Environment], uv.[Value],
	uv.[Version], uv.[UpdateTime], uv.[UpdateUser]
	FROM
	RIFF.[UserConfigKey] uk LEFT OUTER JOIN RIFF.[UserConfigValue] uv ON uk.[UserConfigKeyID] = uv.[UserConfigKeyID]

GO
/****** Object:  View [RIFF].[UserConfigLatestView]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- drop view [RIFF].[UserConfigLatestView]
CREATE VIEW [RIFF].[UserConfigLatestView] AS
	SELECT * FROM RIFF.[UserConfigView] uv
	WHERE CAST(uv.UserConfigKeyID AS VARCHAR(10)) + '_' + CAST(uv.Version AS VARCHAR(10)) + ISNULL(uv.Environment, '*') IN (
	SELECT CAST(uv2.UserConfigKeyID AS VARCHAR(10)) + '_' + CAST(MAX(uv2.Version) AS VARCHAR(10)) + ISNULL(uv2.Environment, '*') 
	FROM RIFF.[UserConfigView] uv2 GROUP BY uv2.UserConfigKeyID, uv2.Environment )

GO
/****** Object:  Table [RIFF].[DispatchQueue]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[DispatchQueue](
	[DispatchQueueID] [bigint] IDENTITY(1,1) NOT NULL,
	[Environment] [varchar](10) NOT NULL,
	[ItemType] [int] NOT NULL,
	[DispatchKey] [varchar](140) NOT NULL,
	[ProcessName] [varchar](100) NOT NULL,
	[GraphInstance] [varchar](20) NULL,
	[ValueDate] [datetime] NULL,
	[Weight] [bigint] NOT NULL,
	[DispatchState] [int] NOT NULL,
	[LastStart] [datetimeoffset](7) NULL,
	[Message] [varchar](200) NULL,
	[ShouldRetry] [bit] NOT NULL,
	[InstructionType] [varchar](200) NULL,
	[InstructionContent] [xml] NULL,
PRIMARY KEY CLUSTERED 
(
	[DispatchQueueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY],
 CONSTRAINT [UQ_DispatchQueue] UNIQUE NONCLUSTERED 
(
	[Environment] ASC,
	[ItemType] ASC,
	[DispatchKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [RIFF].[ProcessLog]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[ProcessLog](
	[LogID] [bigint] IDENTITY(1,1) NOT NULL,
	[Timestamp] [datetimeoffset](7) NOT NULL,
	[Hostname] [varchar](50) NOT NULL,
	[GraphName] [varchar](50) NULL,
	[ProcessName] [varchar](100) NOT NULL,
	[Instance] [varchar](30) NULL,
	[ValueDate] [datetime] NULL,
	[IOTime] [int] NOT NULL,
	[ProcessingTime] [int] NOT NULL,
	[Success] [bit] NOT NULL,
	[Message] [nvarchar](1024) NULL,
	[NumUpdates] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [RIFF].[SystemLog]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[SystemLog](
	[LogID] [bigint] IDENTITY(1,1) NOT NULL,
	[Timestamp] [datetimeoffset](7) NOT NULL,
	[Hostname] [varchar](100) NOT NULL,
	[Level] [varchar](50) NOT NULL,
	[Source] [varchar](255) NOT NULL,
	[Message] [varchar](4000) NOT NULL,
	[Exception] [varchar](2000) NULL,
	[Content] [xml] NULL,
	[Thread] [varchar](50) NOT NULL,
	[AppDomain] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [RIFF].[UserLog]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [RIFF].[UserLog](
	[LogID] [bigint] IDENTITY(1,1) NOT NULL,
	[Area] [varchar](30) NOT NULL,
	[Action] [varchar](50) NOT NULL,
	[Description] [nvarchar](200) NOT NULL,
	[Username] [nvarchar](40) NULL,
	[Processor] [varchar](50) NULL,
	[Timestamp] [datetimeoffset](7) NOT NULL,
	[ValueDate] [datetime] NULL,
	[KeyType] [varchar](50) NULL,
	[KeyReference] [int] NULL,
	[IsUserAction] [bit] NOT NULL,
	[IsWarning] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_CatalogKey_KeyType]    Script Date: 04/08/2017 12:40:09 ******/
CREATE NONCLUSTERED INDEX [IX_CatalogKey_KeyType] ON [RIFF].[CatalogKey]
(
	[KeyType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
GO
/****** Object:  Index [IX_CatalogKeyHash]    Script Date: 04/08/2017 12:40:09 ******/
CREATE NONCLUSTERED INDEX [IX_CatalogKeyHash] ON [RIFF].[CatalogKey]
(
	[KeyHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
GO
/****** Object:  Index [IX_CatalogRootHash]    Script Date: 04/08/2017 12:40:09 ******/
CREATE NONCLUSTERED INDEX [IX_CatalogRootHash] ON [RIFF].[CatalogKey]
(
	[RootHash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_DispatchQueue]    Script Date: 04/08/2017 12:40:09 ******/
CREATE NONCLUSTERED INDEX [IX_DispatchQueue] ON [RIFF].[DispatchQueue]
(
	[Environment] ASC,
	[ItemType] ASC,
	[DispatchKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
GO
ALTER TABLE [RIFF].[CatalogBlob]  WITH CHECK ADD FOREIGN KEY([CatalogEntryID])
REFERENCES [RIFF].[CatalogEntry] ([CatalogEntryID])
GO
ALTER TABLE [RIFF].[CatalogDocument]  WITH CHECK ADD FOREIGN KEY([CatalogEntryID])
REFERENCES [RIFF].[CatalogEntry] ([CatalogEntryID])
GO
ALTER TABLE [RIFF].[CatalogEntry]  WITH CHECK ADD FOREIGN KEY([CatalogKeyID])
REFERENCES [RIFF].[CatalogKey] ([CatalogKeyID])
GO
ALTER TABLE [RIFF].[CatalogStructure]  WITH CHECK ADD FOREIGN KEY([CatalogEntryID])
REFERENCES [RIFF].[CatalogEntry] ([CatalogEntryID])
GO
ALTER TABLE [RIFF].[UserConfigValue]  WITH CHECK ADD FOREIGN KEY([UserConfigKeyID])
REFERENCES [RIFF].[UserConfigKey] ([UserConfigKeyID])
GO
ALTER TABLE [RIFF].[UserRoleMembership]  WITH CHECK ADD FOREIGN KEY([UserRoleID])
REFERENCES [RIFF].[UserRole] ([UserRoleID])
GO
ALTER TABLE [RIFF].[UserRolePermission]  WITH CHECK ADD FOREIGN KEY([UserRoleID])
REFERENCES [RIFF].[UserRole] ([UserRoleID])
GO
/****** Object:  StoredProcedure [RIFF].[GetDocument]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROC [RIFF].[GetDocument] ( @KeyType varchar(100), @SerializedKey varchar(2048), @Version int, @KeyHash int ) AS
BEGIN
	DECLARE @ReserializedKey varchar(2048)
	SELECT @ReserializedKey = CAST(CAST(@SerializedKey as xml) as varchar(2048))

	DECLARE @CatalogKeyID bigint
	SELECT @CatalogKeyID = CatalogKeyID FROM RIFF.CatalogKey
	WHERE KeyType = @KeyType AND [KeyHash] = @KeyHash AND CAST(SerializedKey as varchar(2048)) = @ReserializedKey

	IF @CatalogKeyID IS NULL
	BEGIN
		SELECT TOP 0 * FROM RIFF.DocumentLatestView
	END
	ELSE
	BEGIN
		IF @Version = 0
		BEGIN
			SELECT * FROM RIFF.DocumentLatestView WHERE CatalogKeyID = @CatalogKeyID
		END
		ELSE
		BEGIN
			SELECT * FROM RIFF.DocumentView WHERE CatalogKeyID = @CatalogKeyID AND Version = @Version
		END
	END
END

GO
/****** Object:  StoredProcedure [RIFF].[GetKeyInstances]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [RIFF].[GetKeyInstances] ( @KeyType varchar(50), @SerializedKey varchar(2048), @RootHash int ) as
begin
	DECLARE @ReserializedKey varchar(2048)
	SELECT @ReserializedKey = RIFF.StripInstance(CAST(CAST(@SerializedKey as xml) as varchar(2048)));

	WITH
	XMLNAMESPACES (N'RIFF.Core' as c, 'RIFF.Framework' as f)
	SELECT
		ck.[CatalogKeyID],
		ck.[KeyType],
		cast(ck.[SerializedKey] as varchar(2048)) as [SerializedKey],
		ck.[SerializedKey].value('(//c:GraphInstance/c:Name/text())[1]', 'varchar(100)') as GraphInstanceName,
		ck.[SerializedKey].value('(//c:GraphInstance/c:ValueDate/c:YMD/text())[1]', 'int') as GraphInstanceDate
	FROM [RIFF].[CatalogKey] ck INNER JOIN [RIFF].[DocumentLatestView] dv ON ck.CatalogKeyID = dv.CatalogKeyID
	WHERE
	ck.KeyType = @KeyType
	AND ck.[RootHash] = @RootHash
	AND RIFF.StripInstance(cast(ck.SerializedKey as varchar(2048))) = @ReserializedKey
	AND dv.IsValid = 1
	ORDER BY GraphInstanceDate DESC
end


GO
/****** Object:  StoredProcedure [RIFF].[GetKeyMetadata]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROC [RIFF].[GetKeyMetadata] ( @KeyType varchar(50), @SerializedKey varchar(2048), @KeyHash int ) AS
BEGIN
	DECLARE @ReserializedKey varchar(2048)
	SELECT @ReserializedKey = CAST(CAST(@SerializedKey as xml) as varchar(2048))

	DECLARE @CatalogKeyID bigint
	SELECT @CatalogKeyID = CatalogKeyID FROM RIFF.CatalogKey
	WHERE KeyType = @KeyType AND [KeyHash] = @KeyHash AND CAST(SerializedKey as varchar(2048)) = @ReserializedKey

	IF @CatalogKeyID IS NULL
	BEGIN
		SELECT TOP 0 * FROM RIFF.DocumentLatestView
	END
	ELSE
	BEGIN
		SELECT [CatalogKeyID], [KeyType], [ContentType], [Metadata], [Version], [UpdateTime], [SerializedKey], [IsValid], DATALENGTH([BinaryContent]) as DataSize
		FROM RIFF.DocumentLatestView WHERE CatalogKeyID = @CatalogKeyID
	END
END

GO
/****** Object:  StoredProcedure [RIFF].[OptimizeIndices]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROC [RIFF].[OptimizeIndices] AS
BEGIN
	DECLARE @TableName VARCHAR(255)
	DECLARE @sql NVARCHAR(500)
	DECLARE @fillfactor INT
	SET @fillfactor = 80
	DECLARE TableCursor CURSOR FOR
	SELECT OBJECT_SCHEMA_NAME([object_id])+'.'+name AS TableName
	FROM sys.tables
	OPEN TableCursor
	FETCH NEXT FROM TableCursor INTO @TableName
	WHILE @@FETCH_STATUS = 0
	BEGIN
	SET @sql = 'ALTER INDEX ALL ON ' + @TableName + ' REBUILD WITH (FILLFACTOR = ' + CONVERT(VARCHAR(3),@fillfactor) + ')'
	EXEC (@sql)
	FETCH NEXT FROM TableCursor INTO @TableName
	END
	CLOSE TableCursor
	DEALLOCATE TableCursor
END

GO
/****** Object:  StoredProcedure [RIFF].[PurgeGraphDate]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--drop proc RIFF.PurgeGraphDate
CREATE PROC [RIFF].[PurgeGraphDate] ( @YMD int ) AS
BEGIN
begin tran
	declare @LikeFilter varchar(50)
	select @LikeFilter = '%>' + cast(@YMD as varchar(10)) +'</%'

	delete from riff.CatalogDocument where CatalogEntryID in ( select CatalogEntryID from RIFF.CatalogEntry where CatalogKeyID in 
	(select CatalogKeyID from riff.CatalogKey where cast(serializedkey as nvarchar(max)) like @LikeFilter))

	delete from riff.CatalogEntry where CatalogKeyID in ( select CatalogKeyID from riff.CatalogKey where cast(serializedkey as nvarchar(max)) like @LikeFilter)

	delete from riff.CatalogKey where cast(serializedkey as nvarchar(max)) like @LikeFilter
commit
END

GO
/****** Object:  StoredProcedure [RIFF].[PutUserConfig]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

create proc [RIFF].[PutUserConfig] ( @Section nvarchar(100), @Item nvarchar(100), @Key nvarchar(100), @Description nvarchar(500),
@Environment nvarchar(20), @Value nvarchar(2048), @UpdateUser nvarchar(100) ) as
begin
	begin tran
	declare @keyID int
	select @keyID = UserConfigKeyID from RIFF.UserConfigKey where [Section] = @Section and [Item] = @Item and [Key] = @Key
	if @keyID IS NULL
	begin
		insert into RIFF.UserConfigKey( [Section], [Item], [Key], [Description] ) values ( @Section, @Item, @Key, @Description )
		select @keyID = SCOPE_IDENTITY()
	end
	else
	begin
		if @Description is not null
		begin
			update RIFF.UserConfigKey set [Description] = @Description where [UserConfigKeyID] = @keyID
		end
	end

	declare @version int
	select @version = MAX([Version]) from RIFF.UserConfigLatestView where [UserConfigKeyID] = @keyID and ([Environment] = @Environment OR ([Environment] is null AND @Environment is NULL))
	if @version IS NULL
	begin
		select @version = 1
	end
	else
	begin
		select @version = @version + 1
	end

	insert into RIFF.UserConfigValue ( [UserConfigKeyID], [Environment], [Value], [Version], [UpdateTime], [UpdateUser] )
	values ( @keyID, @Environment, @Value, @version, GETDATE(), @UpdateUser )
	commit
end

GO
/****** Object:  StoredProcedure [RIFF].[SearchKeys]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROC [RIFF].[SearchKeys] ( @KeyType varchar(100) = null, @StartTime datetime = null, @EndTime datetime = null, @LimitResults int, @GraphInstanceDate int = null, @ExcludeFiles bit = 0, @LatestInstanceOnly bit = 0)
AS
BEGIN
	IF @LatestInstanceOnly = 1
	BEGIN
		WITH e AS (
		SELECT
		KV.*,
		ROW_NUMBER() OVER (PARTITION BY KV.KeyType, KV.ContentType, KV.GraphInstanceName, CK.FriendlyString, CK.RootHash ORDER BY KV.GraphInstanceDate DESC) as RN
		FROM RIFF.KeysView KV JOIN RIFF.CatalogKey CK on KV.CatalogKeyID = CK.CatalogKeyID
		WHERE
		(@KeyType IS NULL OR KV.KeyType = @KeyType) AND
		(@ExcludeFiles = 0 OR KV.KeyType <> 'RIFF.Framework.RFFileKey') AND
		(@StartTime IS NULL OR KV.UpdateTime >= @StartTime) AND
		(@EndTime IS NULL OR KV.UpdateTime <= @EndTime) AND
		(@GraphInstanceDate IS NULL OR KV.GraphInstanceDate IS NULL OR KV.GraphInstanceDate <= @GraphInstanceDate)
		)
		SELECT TOP (@LimitResults) * FROM e WHERE RN = 1
	END
	ELSE
	BEGIN
		SELECT TOP (@LimitResults) * FROM RIFF.KeysView KV WHERE
		(@KeyType IS NULL OR KV.KeyType = @KeyType) AND
		(@ExcludeFiles = 0 OR KV.KeyType <> 'RIFF.Framework.RFFileKey') AND
		(@StartTime IS NULL OR KV.UpdateTime >= @StartTime) AND
		(@EndTime IS NULL OR KV.UpdateTime <= @EndTime) AND
		(@GraphInstanceDate IS NULL OR KV.GraphInstanceDate IS NULL OR KV.GraphInstanceDate <= @GraphInstanceDate)
	END
END

GO
/****** Object:  StoredProcedure [RIFF].[TruncateDatabase]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROC [RIFF].[TruncateDatabase] (@dbname varchar(50), @logname varchar(50)) AS
BEGIN
	DBCC SHRINKFILE (@logname, 5)
	DBCC SHRINKDATABASE ( @dbname ,20, NOTRUNCATE )
	DBCC SHRINKDATABASE ( @dbname ,20, TRUNCATEONLY )
END

GO
/****** Object:  StoredProcedure [RIFF].[UpdateDispatchQueue]    Script Date: 04/08/2017 12:40:09 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

--DROP PROC RIFF.UpdateDispatchQueue
CREATE PROC [RIFF].[UpdateDispatchQueue] (
	@Environment varchar(10),
	@ItemType int,
	@DispatchKey varchar(140),
	@ProcessName varchar(100),
	@GraphInstance varchar(20),
	@ValueDate datetime,
	@Weight bigint,
	@DispatchState int,
	@LastStart datetimeoffset,
	@Message varchar(200),
	@ShouldRetry bit,
	@InstructionType varchar(200),
	@InstructionContent xml
) AS
BEGIN
	BEGIN
		IF EXISTS(SELECT 1 from RIFF.DispatchQueue WHERE Environment = @Environment AND ItemType = @ItemType AND DispatchKey = @DispatchKey)
		BEGIN
			UPDATE RIFF.DispatchQueue SET
				ProcessName = ISNULL(@ProcessName, ProcessName),
				GraphInstance = ISNULL(@GraphInstance, GraphInstance),
				ValueDate = ISNULL(@ValueDate, ValueDate),
				Weight = ISNULL(@Weight, Weight),
				DispatchState = ISNULL(@DispatchState, DispatchState),
				LastStart = ISNULL(@LastStart, LastStart),
				Message = ISNULL(@Message, Message),
				ShouldRetry = ISNULL(@ShouldRetry, ShouldRetry),
				InstructionType = ISNULL(@InstructionType, InstructionType),
				InstructionContent = ISNULL(@InstructionContent, InstructionContent)
			WHERE Environment = @Environment AND ItemType = @ItemType AND DispatchKey = @DispatchKey
		END
		ELSE
		BEGIN
			INSERT INTO RIFF.DispatchQueue
			(
				[Environment],
				[ItemType],				
				[DispatchKey],
				[ProcessName],
				[GraphInstance],
				[ValueDate],
				[Weight],
				[DispatchState],
				[LastStart],
				[Message],
				[ShouldRetry],
				[InstructionType],
				[InstructionContent]
			) VALUES (
				@Environment,
				@ItemType,
				@DispatchKey,
				@ProcessName,
				@GraphInstance,
				@ValueDate,
				ISNULL(@Weight, 0),
				@DispatchState,
				@LastStart,
				@Message,
				ISNULL(@ShouldRetry, 0),
				@InstructionType,
				@InstructionContent
			)
		END
	END
END
GO

create table RIFF.MirroredFile
(
	[MirroredFileID] int not null identity(1, 1) primary key,

	[SourceSite] varchar(50) not null,

	[FileSize] int not null,
	[FileName] varchar(200) not null,
	[SourcePath] varchar(300) not null,
	[ModifiedTime] datetime not null,
	[ReceivedTime] datetime not null,
	[IsExtracted] bit not null, -- for containers, 0 if pwd protected as it can be reread
	[MirrorPath] varchar(500) not null unique, -- for retrieving content

	-- named file mapping
	[NamedFileKey] varchar(100) null,
	[Processed] bit null,
	[Message] varchar(100) null,
	[ValueDate] date null,
	[NumRows] int null,
)
go

create proc RIFF.PutMirroredFile (
	@SourceSite varchar(50),
	@FileSize int,
	@FileName varchar(200),
	@SourcePath varchar(300),
	@ModifiedTime datetime,
	@ReceivedTime datetime,
	@IsExtracted bit,
	@MirrorPath varchar(500)
) as
begin
	if exists(select 1 from RIFF.MirroredFile where MirrorPath = @MirrorPath)
	begin
		update RIFF.MirroredFile set 
		SourceSite = ISNULL(@SourceSite, SourceSite),
		FileSize = ISNULL(@FileSize, FileSize),
		FileName = ISNULL(@FileName, FileName),
		SourcePath = ISNULL(SourcePath, SourcePath),
		ModifiedTime = ISNULL(@ModifiedTime, ModifiedTime),
		ReceivedTime = ISNULL(@ReceivedTime, ReceivedTime),
		IsExtracted = ISNULL(@IsExtracted, IsExtracted)
		where MirrorPath = @MirrorPath
	end
	else
	begin
		insert into RIFF.MirroredFile ( SourceSite, FileSize, FileName, SourcePath, ModifiedTime, ReceivedTime, IsExtracted, MirrorPath )
		values (@SourceSite, @FileSize, @FileName, @SourcePath, @ModifiedTime, @ReceivedTime, @IsExtracted, @MirrorPath)
	end
end
go
