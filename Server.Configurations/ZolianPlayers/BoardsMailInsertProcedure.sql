/* 
This procedure is needed to post and send mail

Note: You must run this after ZolianBoardsMail db is populated

Since Subjectline and Message variables are large and player inputs
we use a stored procedure to protect the database against attacks.

Player Mailboxes are created on player creation, with a unique identifier
the packet variable type limits the identifiers to a short (32,767) boxes & boards

Boards are created manually within the database and also contain a unique identifier
*/

USE [ZolianBoardsMail]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertPost]
@BoardId INT, @PostId INT, @Highlighted BIT, @DatePosted DATETIME, @Owner VARCHAR(13), @Sender VARCHAR(13),
@ReadPost BIT, @SubjectLine VARCHAR(100), @Message VARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianBoardsMail].[dbo].[Posts] ([BoardId], [PostId], [Highlighted], [DatePosted], [Owner], [Sender], [ReadPost], [SubjectLine], [Message])
    VALUES	(@BoardId, @PostId, @Highlighted, @DatePosted, @Owner, @Sender, @ReadPost, @SubjectLine, @Message);
END
