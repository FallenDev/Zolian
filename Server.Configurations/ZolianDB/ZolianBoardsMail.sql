USE [ZolianBoardsMail]
GO
ALTER TABLE [dbo].[Posts] DROP CONSTRAINT [DF_Posts_Read]
GO
ALTER TABLE [dbo].[Posts] DROP CONSTRAINT [DF_Posts_DatePosted]
GO
ALTER TABLE [dbo].[Posts] DROP CONSTRAINT [DF_Posts_Highlighted]
GO
ALTER TABLE [dbo].[Posts] DROP CONSTRAINT [DF_Posts_PostId]
GO
ALTER TABLE [dbo].[Posts] DROP CONSTRAINT [DF_Posts_BoardId]
GO
ALTER TABLE [dbo].[Boards] DROP CONSTRAINT [DF_Boards_IsMail]
GO
ALTER TABLE [dbo].[Boards] DROP CONSTRAINT [DF_Boards_Private]
GO
ALTER TABLE [dbo].[Boards] DROP CONSTRAINT [DF_Boards_Serial]
GO
ALTER TABLE [dbo].[Boards] DROP CONSTRAINT [DF_Table_1_MailId]
GO
/****** Object:  Table [dbo].[Posts]    Script Date: 1/4/2024 11:56:09 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Posts]') AND type in (N'U'))
DROP TABLE [dbo].[Posts]
GO
/****** Object:  Table [dbo].[Boards]    Script Date: 1/4/2024 11:56:09 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Boards]') AND type in (N'U'))
DROP TABLE [dbo].[Boards]
GO
/****** Object:  Table [dbo].[Boards]    Script Date: 1/4/2024 11:56:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Boards](
	[BoardId] [bigint] NOT NULL,
	[Serial] [bigint] NOT NULL,
	[Private] [bit] NOT NULL,
	[IsMail] [bit] NOT NULL,
	[Name] [varchar](30) NOT NULL,
 CONSTRAINT [PK_Boards] PRIMARY KEY CLUSTERED 
(
	[BoardId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Posts]    Script Date: 1/4/2024 11:56:10 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Posts](
	[BoardId] [bigint] NOT NULL,
	[PostId] [bigint] NOT NULL,
	[Highlighted] [bit] NOT NULL,
	[DatePosted] [datetime] NOT NULL,
	[Owner] [varchar](13) NOT NULL,
	[Sender] [varchar](13) NOT NULL,
	[ReadPost] [bit] NOT NULL,
	[SubjectLine] [varchar](100) NULL,
	[Message] [varchar](2000) NULL
) ON [PRIMARY]
GO
INSERT [dbo].[Boards] ([BoardId], [Serial], [Private], [IsMail], [Name]) VALUES (0, 0, 0, 0, N'Server Updates')
INSERT [dbo].[Boards] ([BoardId], [Serial], [Private], [IsMail], [Name]) VALUES (1, 0, 0, 0, N'Hunting')
INSERT [dbo].[Boards] ([BoardId], [Serial], [Private], [IsMail], [Name]) VALUES (2, 0, 0, 0, N'Arena Updates')
INSERT [dbo].[Boards] ([BoardId], [Serial], [Private], [IsMail], [Name]) VALUES (3, 0, 0, 0, N'Trash Talk')
INSERT [dbo].[Boards] ([BoardId], [Serial], [Private], [IsMail], [Name]) VALUES (503235460, 503235460, 1, 1, N'Mail')
GO
INSERT [dbo].[Posts] ([BoardId], [PostId], [Highlighted], [DatePosted], [Owner], [Sender], [ReadPost], [SubjectLine], [Message]) VALUES (0, 0, 1, CAST(N'2023-08-06T18:00:16.593' AS DateTime), N'Death', N'Death', 0, N'Beta', N'We are almost near beta as the last of major bugs are addressed!')
INSERT [dbo].[Posts] ([BoardId], [PostId], [Highlighted], [DatePosted], [Owner], [Sender], [ReadPost], [SubjectLine], [Message]) VALUES (1, 0, 1, CAST(N'2023-08-06T18:00:16.593' AS DateTime), N'Death', N'Death', 0, N'', N'Board for hunting recruitment if not avail on Discord')
INSERT [dbo].[Posts] ([BoardId], [PostId], [Highlighted], [DatePosted], [Owner], [Sender], [ReadPost], [SubjectLine], [Message]) VALUES (503235460, 0, 0, CAST(N'2024-01-04T03:35:35.653' AS DateTime), N'Death', N'Alvino', 0, N'Testing', N'Testing abc')
INSERT [dbo].[Posts] ([BoardId], [PostId], [Highlighted], [DatePosted], [Owner], [Sender], [ReadPost], [SubjectLine], [Message]) VALUES (0, 1, 0, CAST(N'2024-01-04T03:35:35.653' AS DateTime), N'Katurina', N'Katurina', 0, N'Hello all!', N'Leaving my mark here. WUAHAHAHA. Love ya''ll. Cheers!')
GO
ALTER TABLE [dbo].[Boards] ADD  CONSTRAINT [DF_Table_1_MailId]  DEFAULT ((0)) FOR [BoardId]
GO
ALTER TABLE [dbo].[Boards] ADD  CONSTRAINT [DF_Boards_Serial]  DEFAULT ((0)) FOR [Serial]
GO
ALTER TABLE [dbo].[Boards] ADD  CONSTRAINT [DF_Boards_Private]  DEFAULT ((0)) FOR [Private]
GO
ALTER TABLE [dbo].[Boards] ADD  CONSTRAINT [DF_Boards_IsMail]  DEFAULT ((0)) FOR [IsMail]
GO
ALTER TABLE [dbo].[Posts] ADD  CONSTRAINT [DF_Posts_BoardId]  DEFAULT ((0)) FOR [BoardId]
GO
ALTER TABLE [dbo].[Posts] ADD  CONSTRAINT [DF_Posts_PostId]  DEFAULT ((0)) FOR [PostId]
GO
ALTER TABLE [dbo].[Posts] ADD  CONSTRAINT [DF_Posts_Highlighted]  DEFAULT ((0)) FOR [Highlighted]
GO
ALTER TABLE [dbo].[Posts] ADD  CONSTRAINT [DF_Posts_DatePosted]  DEFAULT (getdate()) FOR [DatePosted]
GO
ALTER TABLE [dbo].[Posts] ADD  CONSTRAINT [DF_Posts_Read]  DEFAULT ((0)) FOR [ReadPost]
GO
