# Feature Planning Prompt

Plan a new feature for Desktop Calendar Widget before writing any code:

1. **Product Fit**:
   - Check `docs/product/requirements.md` and `docs/product/vision.md`.
   - Is this feature MVP or Future scope? Does it belong to Widget, Full App, or both?
2. **Architecture Impact**:
   - What entities or value objects are needed in `CalendarWidget.Core`?
   - What repository methods or interfaces need to be added?
   - What EF Core mappings are required in `CalendarWidget.Infrastructure`?
   - What ViewModels, Views, or controls are required in `CalendarWidget.Presentation`?
3. **Step-by-step Implementation Order**:
   - Core domain -> Infrastructure persistence -> Presentation UI -> App wiring.
4. **Testing Plan**:
   - Define exact test cases to be written with `MethodName_Condition_ExpectedResult`.
