USE [ZolianWorldMaps]
GO
/****** Object:  Table [dbo].[TemuairSea]    Script Date: 5/26/2023 12:13:49 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TemuairSea]') AND type in (N'U'))
DROP TABLE [dbo].[TemuairSea]
GO
/****** Object:  Table [dbo].[NorthernWaterWay]    Script Date: 5/26/2023 12:13:49 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NorthernWaterWay]') AND type in (N'U'))
DROP TABLE [dbo].[NorthernWaterWay]
GO
/****** Object:  Table [dbo].[MinesPassageWay]    Script Date: 5/26/2023 12:13:49 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MinesPassageWay]') AND type in (N'U'))
DROP TABLE [dbo].[MinesPassageWay]
GO
/****** Object:  Table [dbo].[Lorule]    Script Date: 5/26/2023 12:13:49 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Lorule]') AND type in (N'U'))
DROP TABLE [dbo].[Lorule]
GO
/****** Object:  Table [dbo].[Hyrule]    Script Date: 5/26/2023 12:13:49 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Hyrule]') AND type in (N'U'))
DROP TABLE [dbo].[Hyrule]
GO
/****** Object:  Table [dbo].[FallsPassageWay]    Script Date: 5/26/2023 12:13:49 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FallsPassageWay]') AND type in (N'U'))
DROP TABLE [dbo].[FallsPassageWay]
GO
/****** Object:  Table [dbo].[CorruptedLorule]    Script Date: 5/26/2023 12:13:49 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CorruptedLorule]') AND type in (N'U'))
DROP TABLE [dbo].[CorruptedLorule]
GO
/****** Object:  Table [dbo].[CorruptedLorule]    Script Date: 5/26/2023 12:13:49 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CorruptedLorule](
	[DisplayName] [varchar](50) NULL,
	[PortalName] [varchar](50) NOT NULL,
	[WorldX] [int] NOT NULL,
	[WorldY] [int] NOT NULL,
	[AreaId] [int] NOT NULL,
	[AreaX] [int] NOT NULL,
	[AreaY] [int] NOT NULL,
	[FieldNumber] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FallsPassageWay]    Script Date: 5/26/2023 12:13:49 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FallsPassageWay](
	[DisplayName] [varchar](50) NULL,
	[PortalName] [varchar](50) NOT NULL,
	[WorldX] [int] NOT NULL,
	[WorldY] [int] NOT NULL,
	[AreaId] [int] NOT NULL,
	[AreaX] [int] NOT NULL,
	[AreaY] [int] NOT NULL,
	[FieldNumber] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Hyrule]    Script Date: 5/26/2023 12:13:49 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Hyrule](
	[DisplayName] [varchar](50) NULL,
	[PortalName] [varchar](50) NOT NULL,
	[WorldX] [int] NOT NULL,
	[WorldY] [int] NOT NULL,
	[AreaId] [int] NOT NULL,
	[AreaX] [int] NOT NULL,
	[AreaY] [int] NOT NULL,
	[FieldNumber] [int] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Hyrule] SET (LOCK_ESCALATION = AUTO)
GO
/****** Object:  Table [dbo].[Lorule]    Script Date: 5/26/2023 12:13:49 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lorule](
	[DisplayName] [varchar](50) NULL,
	[PortalName] [varchar](50) NOT NULL,
	[WorldX] [int] NOT NULL,
	[WorldY] [int] NOT NULL,
	[AreaId] [int] NOT NULL,
	[AreaX] [int] NOT NULL,
	[AreaY] [int] NOT NULL,
	[FieldNumber] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MinesPassageWay]    Script Date: 5/26/2023 12:13:49 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MinesPassageWay](
	[DisplayName] [varchar](50) NULL,
	[PortalName] [varchar](50) NOT NULL,
	[WorldX] [int] NOT NULL,
	[WorldY] [int] NOT NULL,
	[AreaId] [int] NOT NULL,
	[AreaX] [int] NOT NULL,
	[AreaY] [int] NOT NULL,
	[FieldNumber] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NorthernWaterWay]    Script Date: 5/26/2023 12:13:49 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NorthernWaterWay](
	[DisplayName] [varchar](50) NULL,
	[PortalName] [varchar](50) NOT NULL,
	[WorldX] [int] NOT NULL,
	[WorldY] [int] NOT NULL,
	[AreaId] [int] NOT NULL,
	[AreaX] [int] NOT NULL,
	[AreaY] [int] NOT NULL,
	[FieldNumber] [int] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TemuairSea]    Script Date: 5/26/2023 12:13:49 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TemuairSea](
	[DisplayName] [varchar](50) NULL,
	[PortalName] [varchar](50) NOT NULL,
	[WorldX] [int] NOT NULL,
	[WorldY] [int] NOT NULL,
	[AreaId] [int] NOT NULL,
	[AreaX] [int] NOT NULL,
	[AreaY] [int] NOT NULL,
	[FieldNumber] [int] NOT NULL
) ON [PRIMARY]
GO
INSERT [dbo].[CorruptedLorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Cascading Falls', N'Cascading Falls', 195, 170, 1201, 9, 2, 4)
INSERT [dbo].[CorruptedLorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Tagor', 300, 155, 662, 22, 98, 4)
INSERT [dbo].[CorruptedLorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Karlopos Beach', N'Karlopos Beach', 410, 200, 4720, 39, 42, 4)
INSERT [dbo].[CorruptedLorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'House Macabre', N'House Macabre', 255, 140, 2052, 10, 17, 4)
INSERT [dbo].[CorruptedLorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Loures', 245, 340, 3012, 13, 8, 4)
INSERT [dbo].[CorruptedLorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Loures Harbor', N'Loures Harbor', 300, 385, 6925, 34, 2, 4)
GO
INSERT [dbo].[FallsPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Cascading Falls', N'Cascading Falls', 195, 170, 1201, 9, 9, 5)
INSERT [dbo].[FallsPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Kasmanium Mines', N'Kasmanium Mines', 160, 195, 660, 24, 47, 5)
INSERT [dbo].[FallsPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Suomi', 195, 80, 3016, 16, 2, 5)
INSERT [dbo].[FallsPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Undine Back', 135, 100, 504, 9, 86, 5)
INSERT [dbo].[FallsPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Astrid', N'Astrid', 105, 150, 3060, 8, 10, 5)
INSERT [dbo].[FallsPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Mount Giragan', N'Mount Giragan', 150, 50, 2120, 37, 7, 5)
GO
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Mileth Front', 90, 250, 3006, 14, 8, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Abel Front', 80, 310, 3014, 13, 13, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Western Woodlands', N'Western Woodlands', 127, 200, 449, 47, 25, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Eastern Woodlands', N'Eastern Woodlands', 100, 280, 600, 11, 22, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Pravat Cave', N'Pravat Cave', 145, 290, 3052, 26, 24, 1)
GO
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Pravat Cave', N'Pravat Cave', 145, 290, 3052, 34, 24, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Arena', N'Arena', 125, 310, 5232, 7, 11, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Piet', 180, 305, 3020, 16, 2, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Loures', 245, 340, 3012, 13, 8, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Loures Harbor', N'Loures Harbor', 300, 385, 6925, 34, 2, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Rionnag', N'Rionnag', 320, 500, 3210, 32, 58, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Lynith Beach', N'Lynith Beach', 365, 530, 6628, 17, 9, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Mehadi Swamp', N'Mehadi Swamp', 220, 390, 3071, 2, 5, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Kasmanium Mines', N'Kasmanium Mines', 160, 195, 660, 25, 47, 2)
GO
INSERT [dbo].[MinesPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Kasmanium Mines', N'Kasmanium Mines', 160, 195, 660, 25, 47, 7)
INSERT [dbo].[MinesPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Cascading Falls', N'Cascading Falls', 195, 170, 1201, 17, 7, 7)
INSERT [dbo].[MinesPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Piet', 180, 305, 3020, 16, 2, 7)
INSERT [dbo].[MinesPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Loures', 245, 340, 3012, 13, 8, 7)
INSERT [dbo].[MinesPassageWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Pravat Cave', N'Pravat Cave', 145, 290, 3052, 26, 24, 7)
GO
INSERT [dbo].[NorthernWaterWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Mileth Side', 90, 250, 3006, 14, 8, 6)
INSERT [dbo].[NorthernWaterWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Suomi', 195, 80, 3016, 16, 2, 6)
INSERT [dbo].[NorthernWaterWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Undine Front', 135, 100, 3008, 13, 16, 6)
INSERT [dbo].[NorthernWaterWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Cascading Falls', N'Cascading Falls', 195, 170, 1201, 9, 2, 6)
INSERT [dbo].[NorthernWaterWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Astrid', N'Astrid', 105, 150, 3060, 8, 10, 6)
INSERT [dbo].[NorthernWaterWay] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Mount Giragan', N'Mount Giragan', 150, 50, 2120, 37, 7, 6)
GO
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Abel Back', 80, 310, 502, 14, 63, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Lynith Beach', N'Lynith Beach', 365, 530, 6628, 17, 9, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Rucesion', 210, 500, 505, 27, 39, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Rionnag Harbor', N'Rionnag Harbor', 320, 520, 3211, 54, 24, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Loures Harbor', N'Loures Harbor', 300, 385, 6925, 34, 2, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Oren', 380, 565, 6228, 57, 168, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Karlopos Beach', N'Karlopos Beach', 410, 200, 4720, 39, 42, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Undine Front', 135, 100, 3008, 13, 16, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Suomi', 195, 80, 3016, 16, 2, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Tagor', 300, 155, 662, 22, 98, 3)
INSERT [dbo].[TemuairSea] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Loures', 245, 340, 3012, 13, 8, 3)
GO
