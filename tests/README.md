# Test data

The public test suite currently uses small synthetic protobuf and internal
representation values authored directly in the test sources. Tests that cover
file serialization create their inputs in temporary directories at run time
and remove them afterward. The test project does not depend on downstream test
data projects or on repository-owned external fixture files.

Before an external fixture is added under `tests`, document its source URL,
source revision, license, and any transformations applied to it. Prefer an
official Substrait example or the smallest synthetic input that demonstrates
the behavior under test.

## Excluded data

The following categories of non-public test data are not included in this
repository:

- Generated benchmark plans without an exact generator revision or per-file
  provenance suitable for publication.
- Query-language conversion inputs outside the portable library's scope.
- Product-specific and distributed-plan fixtures that depend on non-public
  schemas or components.
- Downstream integration corpora and shared test assets used for package-consumer
  and end-to-end compatibility testing.

These exclusions should not be replaced with adapted copies. A corpus may be
reconsidered only after its public provenance and portable dependencies are
independently established.