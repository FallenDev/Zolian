﻿using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

using Darkages.Enums;
using Darkages.Interfaces;

using Microsoft.AppCenter.Crashes;

namespace Darkages.Network.Security;

[Serializable]
public sealed class SecurityProvider : IFormattableNetwork, ICrypto
{
    public static SecurityProvider Instance { get; set; }
    private readonly IReadOnlyList<byte> KeySalts;
    private static readonly ImmutableArray<ImmutableArray<byte>> SaltTree = new[]
    {
        new byte[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
            31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
            60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88,
            89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113,
            114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136,
            137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159,
            160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182,
            183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205,
            206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228,
            229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251,
            252, 253, 254, 255
        }.ToImmutableArray(),
        new byte[]
        {
            128, 127, 129, 126, 130, 125, 131, 124, 132, 123, 133, 122, 134, 121, 135, 120, 136, 119, 137, 118, 138, 117, 139,
            116, 140, 115, 141, 114, 142, 113, 143, 112, 144, 111, 145, 110, 146, 109, 147, 108, 148, 107, 149, 106, 150, 105,
            151, 104, 152, 103, 153, 102, 154, 101, 155, 100, 156, 99, 157, 98, 158, 97, 159, 96, 160, 95, 161, 94, 162, 93,
            163, 92, 164, 91, 165, 90, 166, 89, 167, 88, 168, 87, 169, 86, 170, 85, 171, 84, 172, 83, 173, 82, 174, 81, 175, 80,
            176, 79, 177, 78, 178, 77, 179, 76, 180, 75, 181, 74, 182, 73, 183, 72, 184, 71, 185, 70, 186, 69, 187, 68, 188, 67,
            189, 66, 190, 65, 191, 64, 192, 63, 193, 62, 194, 61, 195, 60, 196, 59, 197, 58, 198, 57, 199, 56, 200, 55, 201, 54,
            202, 53, 203, 52, 204, 51, 205, 50, 206, 49, 207, 48, 208, 47, 209, 46, 210, 45, 211, 44, 212, 43, 213, 42, 214, 41,
            215, 40, 216, 39, 217, 38, 218, 37, 219, 36, 220, 35, 221, 34, 222, 33, 223, 32, 224, 31, 225, 30, 226, 29, 227, 28,
            228, 27, 229, 26, 230, 25, 231, 24, 232, 23, 233, 22, 234, 21, 235, 20, 236, 19, 237, 18, 238, 17, 239, 16, 240, 15,
            241, 14, 242, 13, 243, 12, 244, 11, 245, 10, 246, 9, 247, 8, 248, 7, 249, 6, 250, 5, 251, 4, 252, 3, 253, 2, 254, 1,
            255, 0
        }.ToImmutableArray(),
        new byte[]
        {
            255, 254, 253, 252, 251, 250, 249, 248, 247, 246, 245, 244, 243, 242, 241, 240, 239, 238, 237, 236, 235, 234, 233,
            232, 231, 230, 229, 228, 227, 226, 225, 224, 223, 222, 221, 220, 219, 218, 217, 216, 215, 214, 213, 212, 211, 210,
            209, 208, 207, 206, 205, 204, 203, 202, 201, 200, 199, 198, 197, 196, 195, 194, 193, 192, 191, 190, 189, 188, 187,
            186, 185, 184, 183, 182, 181, 180, 179, 178, 177, 176, 175, 174, 173, 172, 171, 170, 169, 168, 167, 166, 165, 164,
            163, 162, 161, 160, 159, 158, 157, 156, 155, 154, 153, 152, 151, 150, 149, 148, 147, 146, 145, 144, 143, 142, 141,
            140, 139, 138, 137, 136, 135, 134, 133, 132, 131, 130, 129, 128, 127, 126, 125, 124, 123, 122, 121, 120, 119, 118,
            117, 116, 115, 114, 113, 112, 111, 110, 109, 108, 107, 106, 105, 104, 103, 102, 101, 100, 99, 98, 97, 96, 95, 94,
            93, 92, 91, 90, 89, 88, 87, 86, 85, 84, 83, 82, 81, 80, 79, 78, 77, 76, 75, 74, 73, 72, 71, 70, 69, 68, 67, 66, 65,
            64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36,
            35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6,
            5, 4, 3, 2, 1, 0
        }.ToImmutableArray(),
        new byte[]
        {
            255, 1, 254, 2, 253, 3, 252, 4, 251, 5, 250, 6, 249, 7, 248, 8, 247, 9, 246, 10, 245, 11, 244, 12, 243, 13, 242, 14,
            241, 15, 240, 16, 239, 17, 238, 18, 237, 19, 236, 20, 235, 21, 234, 22, 233, 23, 232, 24, 231, 25, 230, 26, 229, 27,
            228, 28, 227, 29, 226, 30, 225, 31, 224, 32, 223, 33, 222, 34, 221, 35, 220, 36, 219, 37, 218, 38, 217, 39, 216, 40,
            215, 41, 214, 42, 213, 43, 212, 44, 211, 45, 210, 46, 209, 47, 208, 48, 207, 49, 206, 50, 205, 51, 204, 52, 203, 53,
            202, 54, 201, 55, 200, 56, 199, 57, 198, 58, 197, 59, 196, 60, 195, 61, 194, 62, 193, 63, 192, 64, 191, 65, 190, 66,
            189, 67, 188, 68, 187, 69, 186, 70, 185, 71, 184, 72, 183, 73, 182, 74, 181, 75, 180, 76, 179, 77, 178, 78, 177, 79,
            176, 80, 175, 81, 174, 82, 173, 83, 172, 84, 171, 85, 170, 86, 169, 87, 168, 88, 167, 89, 166, 90, 165, 91, 164, 92,
            163, 93, 162, 94, 161, 95, 160, 96, 159, 97, 158, 98, 157, 99, 156, 100, 155, 101, 154, 102, 153, 103, 152, 104,
            151, 105, 150, 106, 149, 107, 148, 108, 147, 109, 146, 110, 145, 111, 144, 112, 143, 113, 142, 114, 141, 115, 140,
            116, 139, 117, 138, 118, 137, 119, 136, 120, 135, 121, 134, 122, 133, 123, 132, 124, 131, 125, 130, 126, 129, 127,
            128, 128
        }.ToImmutableArray(),
        new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
            16, 16, 16, 16, 16, 16, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 36, 36, 36, 36, 36, 36, 36,
            36, 36, 36, 36, 36, 36, 36, 36, 36, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 64, 64, 64, 64,
            64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 81, 81, 81, 81, 81, 81, 81, 81, 81, 81, 81, 81, 81, 81, 81, 81, 100,
            100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 121, 121, 121, 121, 121, 121, 121, 121,
            121, 121, 121, 121, 121, 121, 121, 121, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144, 144,
            144, 169, 169, 169, 169, 169, 169, 169, 169, 169, 169, 169, 169, 169, 169, 169, 169, 196, 196, 196, 196, 196, 196,
            196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225, 225,
            225, 225, 225
        }.ToImmutableArray(),
        new byte[]
        {
            0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 58,
            60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 94, 96, 98, 100, 102, 104, 106, 108, 110, 112,
            114, 116, 118, 120, 122, 124, 126, 128, 130, 132, 134, 136, 138, 140, 142, 144, 146, 148, 150, 152, 154, 156, 158,
            160, 162, 164, 166, 168, 170, 172, 174, 176, 178, 180, 182, 184, 186, 188, 190, 192, 194, 196, 198, 200, 202, 204,
            206, 208, 210, 212, 214, 216, 218, 220, 222, 224, 226, 228, 230, 232, 234, 236, 238, 240, 242, 244, 246, 248, 250,
            252, 254, 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54,
            56, 58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 94, 96, 98, 100, 102, 104, 106, 108,
            110, 112, 114, 116, 118, 120, 122, 124, 126, 128, 130, 132, 134, 136, 138, 140, 142, 144, 146, 148, 150, 152, 154,
            156, 158, 160, 162, 164, 166, 168, 170, 172, 174, 176, 178, 180, 182, 184, 186, 188, 190, 192, 194, 196, 198, 200,
            202, 204, 206, 208, 210, 212, 214, 216, 218, 220, 222, 224, 226, 228, 230, 232, 234, 236, 238, 240, 242, 244, 246,
            248, 250, 252, 254
        }.ToImmutableArray(),
        new byte[]
        {
            255, 253, 251, 249, 247, 245, 243, 241, 239, 237, 235, 233, 231, 229, 227, 225, 223, 221, 219, 217, 215, 213, 211,
            209, 207, 205, 203, 201, 199, 197, 195, 193, 191, 189, 187, 185, 183, 181, 179, 177, 175, 173, 171, 169, 167, 165,
            163, 161, 159, 157, 155, 153, 151, 149, 147, 145, 143, 141, 139, 137, 135, 133, 131, 129, 127, 125, 123, 121, 119,
            117, 115, 113, 111, 109, 107, 105, 103, 101, 99, 97, 95, 93, 91, 89, 87, 85, 83, 81, 79, 77, 75, 73, 71, 69, 67, 65,
            63, 61, 59, 57, 55, 53, 51, 49, 47, 45, 43, 41, 39, 37, 35, 33, 31, 29, 27, 25, 23, 21, 19, 17, 15, 13, 11, 9, 7, 5,
            3, 1, 255, 253, 251, 249, 247, 245, 243, 241, 239, 237, 235, 233, 231, 229, 227, 225, 223, 221, 219, 217, 215, 213,
            211, 209, 207, 205, 203, 201, 199, 197, 195, 193, 191, 189, 187, 185, 183, 181, 179, 177, 175, 173, 171, 169, 167,
            165, 163, 161, 159, 157, 155, 153, 151, 149, 147, 145, 143, 141, 139, 137, 135, 133, 131, 129, 127, 125, 123, 121,
            119, 117, 115, 113, 111, 109, 107, 105, 103, 101, 99, 97, 95, 93, 91, 89, 87, 85, 83, 81, 79, 77, 75, 73, 71, 69,
            67, 65, 63, 61, 59, 57, 55, 53, 51, 49, 47, 45, 43, 41, 39, 37, 35, 33, 31, 29, 27, 25, 23, 21, 19, 17, 15, 13, 11,
            9, 7, 5, 3, 1
        }.ToImmutableArray(),
        new byte[]
        {
            255, 253, 251, 249, 247, 245, 243, 241, 239, 237, 235, 233, 231, 229, 227, 225, 223, 221, 219, 217, 215, 213, 211,
            209, 207, 205, 203, 201, 199, 197, 195, 193, 191, 189, 187, 185, 183, 181, 179, 177, 175, 173, 171, 169, 167, 165,
            163, 161, 159, 157, 155, 153, 151, 149, 147, 145, 143, 141, 139, 137, 135, 133, 131, 129, 127, 125, 123, 121, 119,
            117, 115, 113, 111, 109, 107, 105, 103, 101, 99, 97, 95, 93, 91, 89, 87, 85, 83, 81, 79, 77, 75, 73, 71, 69, 67, 65,
            63, 61, 59, 57, 55, 53, 51, 49, 47, 45, 43, 41, 39, 37, 35, 33, 31, 29, 27, 25, 23, 21, 19, 17, 15, 13, 11, 9, 7, 5,
            3, 1, 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56,
            58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 94, 96, 98, 100, 102, 104, 106, 108, 110,
            112, 114, 116, 118, 120, 122, 124, 126, 128, 130, 132, 134, 136, 138, 140, 142, 144, 146, 148, 150, 152, 154, 156,
            158, 160, 162, 164, 166, 168, 170, 172, 174, 176, 178, 180, 182, 184, 186, 188, 190, 192, 194, 196, 198, 200, 202,
            204, 206, 208, 210, 212, 214, 216, 218, 220, 222, 224, 226, 228, 230, 232, 234, 236, 238, 240, 242, 244, 246, 248,
            250, 252, 254
        }.ToImmutableArray(),
        new byte[]
        {
            0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 58,
            60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 94, 96, 98, 100, 102, 104, 106, 108, 110, 112,
            114, 116, 118, 120, 122, 124, 126, 128, 130, 132, 134, 136, 138, 140, 142, 144, 146, 148, 150, 152, 154, 156, 158,
            160, 162, 164, 166, 168, 170, 172, 174, 176, 178, 180, 182, 184, 186, 188, 190, 192, 194, 196, 198, 200, 202, 204,
            206, 208, 210, 212, 214, 216, 218, 220, 222, 224, 226, 228, 230, 232, 234, 236, 238, 240, 242, 244, 246, 248, 250,
            252, 254, 255, 253, 251, 249, 247, 245, 243, 241, 239, 237, 235, 233, 231, 229, 227, 225, 223, 221, 219, 217, 215,
            213, 211, 209, 207, 205, 203, 201, 199, 197, 195, 193, 191, 189, 187, 185, 183, 181, 179, 177, 175, 173, 171, 169,
            167, 165, 163, 161, 159, 157, 155, 153, 151, 149, 147, 145, 143, 141, 139, 137, 135, 133, 131, 129, 127, 125, 123,
            121, 119, 117, 115, 113, 111, 109, 107, 105, 103, 101, 99, 97, 95, 93, 91, 89, 87, 85, 83, 81, 79, 77, 75, 73, 71,
            69, 67, 65, 63, 61, 59, 57, 55, 53, 51, 49, 47, 45, 43, 41, 39, 37, 35, 33, 31, 29, 27, 25, 23, 21, 19, 17, 15, 13,
            11, 9, 7, 5, 3, 1
        }.ToImmutableArray(),
        new byte[]
        {
            255, 30, 30, 30, 30, 30, 30, 30, 30, 59, 59, 59, 59, 59, 59, 59, 59, 86, 86, 86, 86, 86, 86, 86, 86, 111, 111, 111,
            111, 111, 111, 111, 111, 134, 134, 134, 134, 134, 134, 134, 134, 155, 155, 155, 155, 155, 155, 155, 155, 174, 174,
            174, 174, 174, 174, 174, 174, 191, 191, 191, 191, 191, 191, 191, 191, 206, 206, 206, 206, 206, 206, 206, 206, 219,
            219, 219, 219, 219, 219, 219, 219, 230, 230, 230, 230, 230, 230, 230, 230, 239, 239, 239, 239, 239, 239, 239, 239,
            246, 246, 246, 246, 246, 246, 246, 246, 251, 251, 251, 251, 251, 251, 251, 251, 254, 254, 254, 254, 254, 254, 254,
            254, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 254, 254, 254, 254, 254, 254, 254,
            254, 251, 251, 251, 251, 251, 251, 251, 251, 246, 246, 246, 246, 246, 246, 246, 246, 239, 239, 239, 239, 239, 239,
            239, 239, 230, 230, 230, 230, 230, 230, 230, 230, 219, 219, 219, 219, 219, 219, 219, 219, 206, 206, 206, 206, 206,
            206, 206, 206, 191, 191, 191, 191, 191, 191, 191, 191, 174, 174, 174, 174, 174, 174, 174, 174, 155, 155, 155, 155,
            155, 155, 155, 155, 134, 134, 134, 134, 134, 134, 134, 134, 111, 111, 111, 111, 111, 111, 111, 111, 86, 86, 86, 86,
            86, 86, 86, 86, 59, 59, 59, 59, 59, 59, 59, 59, 30, 30, 30, 30, 30, 30, 30, 30
        }.ToImmutableArray()
    }.ToImmutableArray();

    private IReadOnlyList<byte> Salts => SaltTree[Seed];
    public byte[] Salt { get; set; }
    public byte Seed { get; set; }

    public SecurityProvider()
    {
        Instance = this;
        Seed = 0;
        Salt = "UrkcnItnI"u8.ToArray();
        KeySalts = string.IsNullOrEmpty(string.Empty) ? new byte[1024] : GenerateKeySalts(string.Empty);
    }

    public SecurityProvider(byte seed, byte[] salt, string? keySaltSeed = null)
    {
        Seed = seed;
        Salt = salt;
        KeySalts = string.IsNullOrEmpty(keySaltSeed) ? new byte[1024] : GenerateKeySalts(keySaltSeed);
    }

    #region Client

    public bool ShouldOpCodeClientBeEncrypted(byte opCode) => GetClientEncryptionType(opCode) != EncryptionType.None;

    public void Decrypt(NetworkPacket packet, byte opCode, byte sequence)
    {
        var buffer = packet.ToSpan();
        var length = buffer.Length - 7;
        IReadOnlyList<byte> thisKey;
        var a = (ushort)(((buffer[length + 6] << 8) | buffer[length + 4]) ^ 29808);
        var b = (byte)(buffer[length + 5] ^ 35);
        var type = GetClientEncryptionType(opCode);

        switch (type)
        {
            case EncryptionType.Normal:
                length--;
                thisKey = Salt;

                break;
            case EncryptionType.Md5:
                length -= 2;
                thisKey = GenerateKey(a, b);

                break;
            default:
                return;
        }

        for (var i = 0; i < length; ++i)
        {
            var index = (byte)(i / Salt.Length);
            buffer[i] ^= (byte)(Salts[index] ^ thisKey[i % thisKey.Count]);

            if (index != sequence)
                buffer[i] ^= Salts[sequence];
        }

        var slice = buffer[..length];
        //overwrite ref
        buffer = slice;

        if (opCode is 0x39 or 0x3A)
            DecryptDialog(ref buffer);
    }

    public void DecryptDialog(ref Span<byte> buffer)
    {
        var num1 = (byte)(buffer[1] ^ (uint)(byte)(buffer[0] - 45U));
        var num2 = (byte)(num1 + 114U);
        var num3 = (byte)(num1 + 40U);

        buffer[2] ^= num2;
        buffer[3] ^= (byte)(num2 + 1);
        var num4 = (buffer[2] << 8) | buffer[3];

        for (var index = 0; index < num4; index++)
            buffer[4 + index] ^= (byte)(num3 + index);

        var slice = buffer[6..];

        //overwrite ref
        buffer = slice;
    }

    public void Encrypt(NetworkPacket packet, byte opCode, byte sequence)
    {
        var buffer = packet.ToSpan();
        IReadOnlyList<byte> thisKey;
        var a = (ushort)Random.Shared.Next(256, ushort.MaxValue);
        var b = (byte)Random.Shared.Next(100, byte.MaxValue);
        var type = ServerEncryptionType(opCode);

        switch (type)
        {
            case EncryptionType.Normal:
                thisKey = Salt;

                break;
            case EncryptionType.Md5:
                thisKey = GenerateKey(a, b);

                break;
            default:
                return;
        }

        for (var i = 0; i < buffer.Length; i++)
        {
            var index = (byte)(i / Salt.Length);
            buffer[i] ^= (byte)(Salts[index] ^ thisKey[i % thisKey.Count]);

            if (index != sequence)
                buffer[i] ^= Salts[sequence];
        }

        var newBuffer = new Span<byte>(new byte[buffer.Length + 3]);
        buffer.CopyTo(newBuffer);

        newBuffer[^3] = (byte)((byte)a ^ 116);
        newBuffer[^2] = (byte)(b ^ 36U);
        newBuffer[^1] = (byte)((byte)(a >> 8) ^ 100);

        //overwrite ref
        buffer = newBuffer;
    }

    public EncryptionType GetClientEncryptionType(byte opCode) =>
        opCode switch
        {
            0x00 => EncryptionType.None,
            0x10 => EncryptionType.None,
            0x48 => EncryptionType.None,
            0x02 => EncryptionType.Normal,
            0x03 => EncryptionType.Normal,
            0x04 => EncryptionType.Normal,
            0x0B => EncryptionType.Normal,
            0x26 => EncryptionType.Normal,
            0x2D => EncryptionType.Normal,
            0x3A => EncryptionType.Normal,
            0x42 => EncryptionType.Normal,
            0x43 => EncryptionType.Normal,
            0x4B => EncryptionType.Normal,
            0x57 => EncryptionType.Normal,
            0x62 => EncryptionType.Normal,
            0x68 => EncryptionType.Normal,
            0x71 => EncryptionType.Normal,
            0x73 => EncryptionType.Normal,
            0x7B => EncryptionType.Normal,
            _ => EncryptionType.Md5
        };

    #endregion

    #region Server

    public bool ShouldOpCodeServerBeEncrypted(byte opCode) => ServerEncryptionType(opCode) != EncryptionType.None;

    public EncryptionType ServerEncryptionType(byte opCode) =>
        opCode switch
        {
            0x00 => EncryptionType.None,
            0x03 => EncryptionType.None,
            0x40 => EncryptionType.None,
            0x7E => EncryptionType.None,
            0x01 => EncryptionType.Normal,
            0x02 => EncryptionType.Normal,
            0x0A => EncryptionType.Normal,
            0x56 => EncryptionType.Normal,
            0x60 => EncryptionType.Normal,
            0x62 => EncryptionType.Normal,
            0x66 => EncryptionType.Normal,
            0x6F => EncryptionType.Normal,
            _ => EncryptionType.Md5
        };

    #endregion

    //public void Transform(NetworkPacket packet)
    //{
    //    try
    //    {
    //        Parallel.For(0, packet.Data.Length, delegate (int i)
    //        {
    //            var mod = (i / Salt.Length) & 0xFF;

    //            packet.Data[i] ^= (byte)(
    //                Salt[i % Salt.Length] ^
    //                SaltTree[Seed][packet.Sequence] ^
    //                SaltTree[Seed][mod]);

    //            if (packet.Sequence == mod) packet.Data[i] ^= SaltTree[Seed][packet.Sequence];
    //        });
    //    }
    //    catch (Exception e)
    //    {
    //        ServerSetup.Logger(e.ToString());
    //        Crashes.TrackError(e);
    //    }
    //}

    public byte[] GenerateKey(ushort a, byte b)
    {
        var key = new byte[9];

        for (var i = 0; i < 9; i++)
            key[i] = KeySalts[(i * (9 * i + b * b) + a) % KeySalts.Count];

        return key;
    }

    /// <summary>
    /// Used for encrypting/decrypting MD5
    /// </summary>
    public byte[] GenerateKeySalts(string seed)
    {
        var saltTable = GetMd5Hash(GetMd5Hash(seed));

        for (var i = 0; i < 31; i++)
            saltTable += GetMd5Hash(saltTable);

        return Encoding.ASCII.GetBytes(saltTable);
    }

    public string GetMd5Hash(string value) => BitConverter.ToString(MD5.HashData(Encoding.ASCII.GetBytes(value)))
            .Replace("-", string.Empty)
            .ToLower();

    public void Serialize(NetworkPacketReader reader)
    {
        Seed = reader.ReadByte();
        Salt = reader.ReadBytes(reader.ReadByte());
    }

    public void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Seed);
        writer.Write((byte)Salt.Length);
        writer.Write(Salt);
    }
}