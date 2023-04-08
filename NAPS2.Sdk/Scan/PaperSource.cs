﻿namespace NAPS2.Scan;

public enum PaperSource
{
    /// <summary>
    /// Use a supported paper source for the device. Depending on the driver this usually prefers Flatbed if supported.
    /// </summary>
    Auto,

    /// <summary>
    /// Use the flatbed component of the scanner to scan a single page.
    /// </summary>
    Flatbed,

    /// <summary>
    /// Use the automatic document feeder component of the scanner, potentially scanning multiple pages.
    /// </summary>
    Feeder,

    /// <summary>
    /// Use the automatic document feeder component of the scanner with double-sided scanning, potentially scanning
    /// multiple pages.
    /// </summary>
    Duplex
}