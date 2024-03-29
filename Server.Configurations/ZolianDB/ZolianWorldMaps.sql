USE [ZolianWorldMaps]
GO
/****** Object:  Table [dbo].[Lorule]    Script Date: 2/17/2024 1:44:36 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Lorule]') AND type in (N'U'))
DROP TABLE [dbo].[Lorule]
GO
/****** Object:  Table [dbo].[Hyrule]    Script Date: 2/17/2024 1:44:36 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Hyrule]') AND type in (N'U'))
DROP TABLE [dbo].[Hyrule]
GO
/****** Object:  Table [dbo].[HighSeas]    Script Date: 2/17/2024 1:44:36 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HighSeas]') AND type in (N'U'))
DROP TABLE [dbo].[HighSeas]
GO
/****** Object:  Table [dbo].[HiddenValley]    Script Date: 2/17/2024 1:44:36 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HiddenValley]') AND type in (N'U'))
DROP TABLE [dbo].[HiddenValley]
GO
/****** Object:  Table [dbo].[HiddenValley]    Script Date: 2/17/2024 1:44:36 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HiddenValley](
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
/****** Object:  Table [dbo].[HighSeas]    Script Date: 2/17/2024 1:44:36 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HighSeas](
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
/****** Object:  Table [dbo].[Hyrule]    Script Date: 2/17/2024 1:44:36 AM ******/
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
/****** Object:  Table [dbo].[Lorule]    Script Date: 2/17/2024 1:44:36 AM ******/
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
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Astrid', N'Astrid', 300, 450, 3060, 8, 10, 3)
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Mount Giragan', N'Mount Giragan', 105, 150, 2120, 37, 7, 3)
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Undine Back', 330, 160, 504, 10, 87, 3)
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Undine', 340, 240, 3008, 13, 16, 3)
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Suomi', 195, 390, 3016, 16, 2, 3)
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Mehadi Swamp', N'Mehadi Swamp', 195, 130, 3071, 2, 5, 3)
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Cascading Falls', N'Cascading Falls', 150, 500, 1201, 9, 1, 3)
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'North Pole', N'North Pole', 85, 550, 7050, 2, 11, 3)
INSERT [dbo].[HiddenValley] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Front Lines', N'Front Lines', 260, 515, 701, 9, 18, 3)
GO
INSERT [dbo].[HighSeas] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Dubhaim Castle', N'Dubhaim', 100, 350, 340, 25, 47, 4)
INSERT [dbo].[HighSeas] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Cascading Falls', N'Cascading Falls', 90, 170, 1201, 9, 9, 4)
INSERT [dbo].[HighSeas] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Karlopos Beach', N'Karlopos Beach', 250, 120, 4720, 39, 42, 4)
INSERT [dbo].[HighSeas] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Rucesion', 230, 340, 505, 27, 39, 4)
GO
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Mileth Front', 243, 315, 3006, 14, 8, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Abel Front', 230, 210, 3014, 13, 13, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Western Woodlands', N'Western Woodlands', 195, 225, 449, 47, 25, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Eastern Woodlands', N'Eastern Woodlands', 235, 375, 600, 11, 22, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Pravat Cave', N'Pravat Cave', 175, 185, 3052, 26, 24, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Piet', 180, 325, 3020, 16, 2, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Kasmanium Mines', N'Kasmanium Mines', 118, 235, 660, 25, 47, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Loures', 125, 370, 3012, 13, 8, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Rionnag', 267, 357, 3210, 32, 58, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Cascading Falls', N'Cascading Falls', 140, 120, 1201, 18, 8, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Mileth Side', 210, 300, 500, 54, 2, 1)
INSERT [dbo].[Hyrule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Loures Harbor', N'Loures Harbor', 150, 440, 6925, 34, 2, 1)
GO
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Pravat Cave', N'Pravat Cave', 175, 245, 3052, 34, 24, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Arena', N'Arena', 225, 575, 5232, 7, 11, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Cascading Falls', N'Cascading Falls', 140, 120, 1201, 18, 8, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Abel Docks', N'Abel Docks', 219, 140, 502, 14, 64, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Loures Harbor', N'Loures Harbor', 150, 440, 6925, 34, 2, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Rionnag Harbor', N'Rionnag Harbor', 300, 395, 3211, 54, 23, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Lynith Beach', N'Lynith Beach', 300, 545, 6628, 17, 9, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Loures', 125, 370, 3012, 13, 8, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Rionnag', 267, 357, 3210, 32, 58, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Tagor', 330, 175, 662, 22, 98, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'', N'Oren', 370, 445, 6228, 57, 168, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Mileth Harbor', N'Mileth Harbor', 243, 260, 499, 44, 16, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'House Macabre', N'House Macabre', 425, 115, 2052, 10, 17, 2)
INSERT [dbo].[Lorule] ([DisplayName], [PortalName], [WorldX], [WorldY], [AreaId], [AreaX], [AreaY], [FieldNumber]) VALUES (N'Shinewood Forest', N'Shinewood', 350, 135, 542, 13, 22, 2)
GO
