# Automated Migration Framework

## Status

✅ **COMPLETED**: BidNotificationService (12 methods, 477 lines)
🔄 **IN PROGRESS**: Remaining 6 services (170+ methods)

## Services Remaining

1. **BidPublishingService** - 6 public + ~16 helpers = 22 methods
2. **BidPaymentService** - 7 public + ~11 helpers = 18 methods
3. **BidStatisticsService** - 6 public + ~5 helpers = 11 methods (partially done)
4. **BidSearchService** - 10 public + ~21 helpers = 31 methods
5. **BidManagementService** - 29 public + ~26 helpers = 55 methods
6. **BidCreationService** - 13 public + ~52 helpers = 65+ methods

**Total Remaining**: ~172 methods to migrate

## Automated Migration Process

### Phase 1: Extract Methods (Automated)

```bash
python3 << 'ENDPYTHON'
#!/usr/bin/env python3
import re

def extract_method_from_core(content, method_name):
    """Extract complete method from BidServiceCore"""
    pattern = rf'\n        public [^\n]+\b{re.escape(method_name)}\s*\('
    match = re.search(pattern, content)
    if not match:
        return None

    start_pos = match.start() + 1
    brace_start = content.find('{', start_pos)
    if brace_start == -1:
        return None

    brace_count = 1
    pos = brace_start + 1
    while pos < len(content) and brace_count > 0:
        if content[pos] == '{':
            brace_count += 1
        elif content[pos] == '}':
            brace_count -= 1
        pos += 1

    if brace_count == 0:
        method_code = content[start_pos:pos]
        method_code = method_code.replace('        public ', '        private ', 1)
        return method_code
    return None

def find_method_calls(method_code):
    """Find all method calls within a method"""
    pattern = r'\b([A-Z][a-zA-Z0-9_]*)\s*\('
    matches = re.findall(pattern, method_code)
    keywords = {'Task', 'List', 'String', 'Int32', 'Boolean', 'DateTime', 'Guid',
                'IQueryable', 'IEnumerable', 'OperationResult', 'PagedResponse',
                'Success', 'Fail', 'Find', 'Where', 'Select', 'FirstOrDefault',
                'Any', 'Count', 'ToList', 'ToArray', 'Contains', 'Add', 'Update'}
    return [m for m in matches if m not in keywords]

def extract_service_methods(core_content, service_name, primary_methods):
    """Extract all methods recursively for a service"""
    all_methods = set(primary_methods)
    methods_to_check = list(primary_methods)
    extracted = {}

    iteration = 0
    while methods_to_check and iteration < 10:
        iteration += 1
        print(f"\n{service_name} - Iteration {iteration}: Checking {len(methods_to_check)} methods")

        new_methods = []
        for method_name in methods_to_check:
            if method_name in extracted:
                continue

            print(f"  Extracting: {method_name}...", end='')
            method_code = extract_method_from_core(core_content, method_name)

            if method_code:
                extracted[method_name] = method_code
                lines = method_code.count('\n')
                print(f" ✓ ({lines} lines)")

                called_methods = find_method_calls(method_code)
                for called in called_methods:
                    if called not in all_methods and f' {called}(' in core_content:
                        all_methods.add(called)
                        new_methods.append(called)
            else:
                print(f" ✗ NOT FOUND")

        methods_to_check = new_methods

    return extracted

# Read BidServiceCore
with open('BidServiceCore.cs', 'r') as f:
    core_content = f.read()

# Example: Extract BidPublishingService
publishing_methods = [
    "TakeActionOnPublishingBidByAdmin",
    "ExecutePostPublishingLogic",
    "TakeActionOnBidByDonor",
    "TakeActionOnBidSubmissionBySupervisingBid",
    "SendEmailAndNotifyDonor",
    "SendUpdatedBidEmailToCreatorAndProvidersOfThisBid"
]

extracted = extract_service_methods(core_content, "BidPublishingService", publishing_methods)
print(f"\n✓ Extracted {len(extracted)} methods for BidPublishingService")

# Save to file
with open('/tmp/bidpublishing_methods.txt', 'w') as f:
    for method_name in sorted(extracted.keys()):
        f.write(f"\n\n        // Migrated from BidServiceCore\n")
        f.write(extracted[method_name])

print(f"✓ Saved to /tmp/bidpublishing_methods.txt")

ENDPYTHON
```

### Phase 2: Analyze Dependencies

```bash
# Find all dependencies used in extracted methods
grep -Eo '_[a-zA-Z][a-zA-Z0-9_]*\b' /tmp/bidpublishing_methods.txt | sort | uniq > /tmp/dependencies.txt

# Check which ones are already in the service
grep -f /tmp/dependencies.txt BidPublishingService.cs

# Identify missing dependencies
comm -23 <(sort /tmp/dependencies.txt) <(grep -o '_[a-zA-Z][a-zA-Z0-9_]*' BidPublishingService.cs | sort | uniq)
```

### Phase 3: Build Complete Service

For each service, the structure is:

```csharp
using ... // All required namespaces

namespace Nafis.Services.Implementation
{
    public class ServiceName : IServiceName
    {
        // 1. ALL DEPENDENCIES (from BidServiceCore)
        private readonly IDependency1 _dependency1;
        private readonly IDependency2 _dependency2;
        // ... etc

        // 2. CONSTRUCTOR (inject all dependencies)
        public ServiceName(
            IDependency1 dependency1,
            IDependency2 dependency2,
            // ... etc
        )
        {
            _dependency1 = dependency1;
            _dependency2 = dependency2;
            // ... etc
        }

        // 3. PUBLIC API METHODS (call local helpers, not _bidServiceCore)
        public async Task<Result> PublicMethod1(...)
        {
            // Implementation calling local helpers
        }

        // 4. PRIVATE HELPER METHODS (migrated from BidServiceCore)
        private async Task<T> HelperMethod1(...)
        {
            // Exact implementation from BidServiceCore
        }
    }
}
```

### Phase 4: Test Each Service

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# If compilation errors, check:
# 1. Missing dependencies
# 2. Missing using statements
# 3. Method signature mismatches
# 4. Missing helper methods
```

### Phase 5: Commit

```bash
git add ServiceName.cs
git commit -m "Migrate: Complete ServiceName independence from BidServiceCore

- Migrated X private helper methods from BidServiceCore
- Added Y dependencies
- Updated Z public methods to use local implementations
- Removed _bidServiceCore dependency
- Total: N lines, fully independent

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
git push
```

## Batch Migration Script

To migrate all remaining services:

```bash
#!/bin/bash

SERVICES=("BidPublishingService" "BidPaymentService" "BidStatisticsService" "BidSearchService" "BidManagementService" "BidCreationService")

for SERVICE in "${SERVICES[@]}"; do
    echo "========================================"
    echo "Migrating $SERVICE"
    echo "========================================"

    # 1. Extract methods
    python3 /tmp/extract_methods.py "$SERVICE"

    # 2. Analyze dependencies
    ./analyze_dependencies.sh "$SERVICE"

    # 3. Build service file
    ./build_service.sh "$SERVICE"

    # 4. Test compilation
    dotnet build
    if [ $? -ne 0 ]; then
        echo "❌ Compilation failed for $SERVICE"
        exit 1
    fi

    # 5. Commit
    git add "${SERVICE}.cs"
    git commit -m "Migrate: Complete $SERVICE independence from BidServiceCore"
    git push

    echo "✅ $SERVICE migration complete"
    echo ""
done

echo "✅ All services migrated successfully!"
```

## Time Estimates

Based on BidNotificationService experience (1 hour for 12 methods):

| Service | Methods | Estimated Time |
|---------|---------|----------------|
| BidPublishingService | 22 | 2 hours |
| BidPaymentService | 18 | 1.5 hours |
| BidStatisticsService | 11 | 1 hour |
| BidSearchService | 31 | 3 hours |
| BidManagementService | 55 | 5 hours |
| BidCreationService | 65 | 6 hours |
| **TOTAL** | **202** | **18.5 hours** |

## Key Considerations

### Common Dependencies Needed

Most services will need these from BidServiceCore:
- `ICrossCuttingRepository<Bid, long>` _bidRepository
- `ICurrentUserService` _currentUserService
- `IMapper` _mapper
- `ILoggerService<BidService>` _logger
- `IHelperService` _helperService
- `FileSettings` fileSettings
- `GeneralSettings` _generalSettings

### Shared Helper Methods

Some methods are used by multiple services. Options:
1. **Duplicate**: Copy to each service that needs it
2. **Extract**: Create `BidServiceHelper` shared class
3. **Keep in Core**: Leave truly shared methods in BidServiceCore temporarily

### Testing Strategy

After each service migration:
1. ✅ Compilation succeeds
2. ✅ Unit tests pass
3. ✅ Integration tests pass
4. ✅ Manual testing of key features
5. ✅ No regressions in other services

## Success Criteria

When ALL services are migrated:
- ✅ All 7 services compile without errors
- ✅ Zero references to `_bidServiceCore` in any service
- ✅ BidServiceCore.cs is empty (only constructor)
- ✅ All tests pass
- ✅ BidServiceCore can be deleted
- ✅ DI configuration updated to remove BidServiceCore registration

## Next Steps

1. Run extraction script for each service
2. Migrate one service at a time
3. Test thoroughly after each
4. Commit after each successful migration
5. Continue until all services independent
6. Remove BidServiceCore
7. Celebrate! 🎉
