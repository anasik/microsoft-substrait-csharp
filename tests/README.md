# Test data

The public test suite currently uses small synthetic protobuf and internal
representation values authored directly in the test sources. Tests that cover
file serialization create their inputs in temporary directories at run time
and remove them afterward. The test project does not depend on the downstream
`TestData` project or on repository-owned external fixture files.

Before an external fixture is added under `tests`, document its source URL,
source revision, license, and any transformations applied to it. Prefer an
official Substrait example or the smallest synthetic input that demonstrates
the behavior under test.

## Excluded downstream data

The following downstream test data was reviewed but is not included in this
repository:

- Isthmus-generated TPC-H JSON and protobuf plans, including FP64 variants.
  Existing generation commands do not identify an exact Isthmus revision or
  establish per-file provenance suitable for publication.
- query-language-generated plans and `query-language integration` inputs. query-language conversion is outside
  the portable library's development scope.
- product-specific, distributed-plan, product-specific, and product-specific fixtures. These depend
  on product-specific schemas or non-public components and remain outside the
  public contribution scope.
- downstream and downstream integration corpora and shared `TestData` assets. These remain in
  downstream for package-consumer and end-to-end compatibility testing.

These exclusions should not be replaced with adapted copies. A corpus may be
reconsidered only after its public provenance and portable dependencies are
independently established.