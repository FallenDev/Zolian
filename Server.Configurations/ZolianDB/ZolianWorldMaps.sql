USE [ZolianWorldMaps]
GO
/****** Object:  Table [dbo].[Lorule]    Script Date: 12/11/2023 12:54:37 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Lorule]') AND type in (N'U'))
DROP TABLE [dbo].[Lorule]
GO
/****** Object:  Table [dbo].[Hyrule]    Script Date: 12/11/2023 12:54:37 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Hyrule]') AND type in (N'U'))
DROP TABLE [dbo].[Hyrule]
GO
/****** Object:  Table [dbo].[HighSeas]    Script Date: 12/11/2023 12:54:37 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HighSeas]') AND type in (N'U'))
DROP TABLE [dbo].[HighSeas]
GO
/****** Object:  Table [dbo].[HiddenValley]    Script Date: 12/11/2023 12:54:37 PM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HiddenValley]') AND type in (N'U'))
DROP TABLE [dbo].[HiddenValley]
GO
