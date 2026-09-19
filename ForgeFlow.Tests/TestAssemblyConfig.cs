

// Disable parallel test execution — many tests share static EntityIdFactory state
// via ResetForTesting() and cannot safely run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
