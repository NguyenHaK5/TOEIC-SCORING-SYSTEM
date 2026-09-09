# PHASE 7 – PART 5: RATER — Integration Guide

## 1. Mục tiêu Part này

Danh sách bài Writing đã submit cho Rater chấm (lọc Pending/Rated/All +
theo dạng câu), trang chi tiết 1 bài (câu trả lời + đánh giá AI hiện hành +
Rubric ACTIVE + lịch sử Human đã chấm) với form nhập điểm theo từng
Criterion, và trang so sánh AI vs Human sắp xếp theo mức lệch điểm giảm
dần.

**Demo cuối Part:** Rater chấm 1 bài qua UI → thấy điểm AI cạnh điểm vừa
chấm, sắp theo mức lệch.

## 2. Rà soát trước khi code

### PHASE7-ISSUE-15 — Shape DTO của Rater API là giả định (cùng loại rủi ro với Part 4)
**Problem:** `phase-tracker.md` Phase 4 Part 1-2 chỉ mô tả bằng lời, không
có ví dụ JSON/curl:
- Part 1: `"GET /rater/writing-answers | Danh sách bài Writing đã submit,
  lọc status=pending|rated|all, questionType, phân trang"`,
  `"GET /rater/writing-answers/{candidateAnswerId} | Chi tiết: câu trả lời
  + AI assessment + Rubric ACTIVE + lịch sử Human"`,
  `"POST /rater/writing-answers/{candidateAnswerId}/score | Nhập điểm theo
  Criterion → tính & lưu HumanAssessment"`.
- Part 2: `"dataset cặp (AI, Human) theo Criterion, endpoint
  GET /rater/writing-answers/comparison, sắp xếp theo |OverallDifference|
  giảm dần"`.
**Severity:** Medium — tên endpoint và hành vi tổng thể có bằng chứng trực
tiếp (khác PHASE7-ISSUE-10 ở Part 3 — endpoint tồn tại chắc chắn, chỉ
DTO field name là giả định).
**Decision:** Field name suy ra từ schema đã chốt ở PHASE0 mục E.2.9 (
`CandidateAnswers`), E.2.12 (`WritingAssessments`), E.2.13
(`HumanAssessments`), E.2.14 (`AssessmentCriterionScores`) — cô lập trong
`types/rater.ts`, `types/rater-comparison.ts`, `lib/rater/rater-api.ts`.
**Reason:** Cùng nguyên tắc đã áp dụng nhất quán từ PHASE7-ISSUE-07 (Part
2) — sai ở đâu chỉ sửa đúng 1-2 file, không lan ra UI.

**Không có issue kiến trúc mới nào phát sinh ở Part này** — toàn bộ pattern
(React Query, form component con khởi tạo state trực tiếp từ prop để tránh
`set-state-in-effect`, `ConfirmButton`, `StatusBadge`-style) đều tái sử
dụng nguyên vẹn từ Part 2-4, không cần quyết định kiến trúc mới.

## 3. Kiến trúc

```
app/rater/writing-answers/                    list — tab Pending/Rated/All + filter questionType
app/rater/writing-answers/[candidateAnswerId]/  detail — câu trả lời + AI + lịch sử Human + form chấm
app/rater/comparison/                          so sánh AI vs Human, sort theo |difference|

lib/rater/rater-api.ts   list/get/submitScore/comparison

types/
├── rater.ts             WritingAnswerListItem, WritingAnswerDetail, SubmitHumanScorePayload
└── rater-comparison.ts  AiHumanComparisonItem
```

### Form chấm điểm — dynamic theo Rubric ACTIVE
`ScoringForm` (trong `[candidateAnswerId]/page.tsx`) sinh input số cho
**từng Criterion** của `activeRubric.criteria` (không hard-code theo Q1-5/
Q6-7/Q8 — đúng nguyên tắc PHASE3-ISSUE-02 đã chốt ở backend: rubric sinh
động). Mỗi input giới hạn `min`/`max` theo `RubricCriterionDefinition`,
khởi tạo ở điểm giữa khoảng làm gợi ý trung tính. Overall Score **không
được tính ở Frontend** — gửi nguyên `criterionScores` lên
`POST .../score`, để `WritingScoringEngine` (domain logic .NET, Phase 3
Part 2) tính — đúng nguyên tắc #3/#5 prompt gốc ("Không trộn AI Analysis
với Scoring Engine", áp dụng tương tự cho Human).

State form khởi tạo trực tiếp từ `activeRubric` (không qua `useEffect`) —
cùng pattern tránh lint `set-state-in-effect` đã dùng từ Part 2.

## 4. Đã tự verify trong sandbox

| Kiểm tra | Kết quả |
|---|---|
| `npm run build` (production) | ✅ Thành công, 0 lỗi (29 route) |
| `npx eslint .` | ✅ 0 lỗi, 0 warning ngay lần đầu |
| RBAC: `/rater/*` yêu cầu đúng RATER (CANDIDATE → `/unauthorized`) | ✅ curl test |
| RBAC: route khác không hồi quy | ✅ curl test |
| Danh sách: tab lọc + dropdown dạng câu render đúng | ✅ Chụp ảnh qua Playwright + mock response |
| Chi tiết: đánh giá AI (điểm/model/feedback/breakdown tiêu chí) hiển thị đúng | ✅ |
| Form chấm điểm: sinh đúng 3 input theo Rubric giả lập, giữ giá trị khi điền | ✅ Verify bằng `page.fill()` + screenshot |
| So sánh AI vs Human: màu mức lệch đúng ngưỡng (đỏ ≥15/cam 7-14/xanh <7) | ✅ |

**Lưu ý:** dùng Playwright chặn response (`page.route`) trả JSON giả lập
đúng theo shape đã định nghĩa — xác nhận ĐÚNG hành vi Frontend, KHÔNG xác
nhận shape đó khớp backend thật (xem PHASE7-ISSUE-15).

## 5. Chưa làm ở Part này (đúng phạm vi, để Part sau)

- Phân công Rater (assignment) — theo PHASE4-ISSUE-01 đã chốt ở backend
  (MVP không có bảng assignment, mọi Rater xem/chấm được mọi bài), Frontend
  không cần UI phân công.
- Sửa/xoá 1 lần chấm Human đã lưu — theo thiết kế audit-trail (1:N, mỗi
  lần chấm tạo bản ghi mới, xem PHASE0 ISSUE-04), form chấm điểm ở Part
  này luôn tạo đánh giá **mới**, không sửa đánh giá cũ.
- Filter so sánh AI vs Human theo `questionType`/ngưỡng lệch tối thiểu —
  API đã hỗ trợ (`raterApi.comparison` nhận `questionType`), nhưng UI lọc
  chưa thêm ở Part này để giữ phạm vi gọn — có thể bổ sung nhanh nếu Dragon
  cần.

## 6. Việc BẮT BUỘC Dragon cần làm trước khi coi Part 5 là "Done"

1. **Chạy `npm run dev` với backend .NET Phase 4 Part 1-2 thật đang chạy
   song song** — Part đầu tiên phụ thuộc riêng module Rater.
2. **Đối chiếu shape thật của cả 4 endpoint** (list/detail/score/
   comparison) — ưu tiên `POST .../score` trước (request body sai sẽ chặn
   hoàn toàn chức năng chấm điểm).
3. **Test luồng đầy đủ:** Candidate nộp 1 bài Writing → đợi AI chấm xong
   (Phase 3 Part 4 background job) → Rater vào `/rater/writing-answers` →
   thấy bài ở tab "Chờ chấm" → chấm điểm → xác nhận chuyển sang tab "Đã
   chấm" → vào `/rater/comparison` thấy cặp AI/Human vừa tạo.
4. **Xác nhận `IWritingScoreResolver`** (đã có ở Phase 4 Part 2) trả đúng
   điểm Human vừa chấm làm điểm chính thức cho Candidate xem ở Result page
   (Part 4) — đây là điểm nối giữa Part 4 và Part 5, nên test xuyên suốt cả
   2 vai trò.
