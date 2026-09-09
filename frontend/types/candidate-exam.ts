/**
 * ⚠️ GIẢ ĐỊNH LỚN NHẤT của Part 3 — xem PHASE7-ISSUE-10 trong
 * PHASE7_PART3_INTEGRATION_GUIDE.md.
 *
 * README.md gốc (mục "Part 3 — Exam Management CRUD") ghi rõ: toàn bộ
 * endpoint trong `ExamsController` (bao gồm `GET /exams`, `GET /exams/{id}`)
 * yêu cầu `[Authorize(Roles=ADMIN)]`. Điều đó có nghĩa Candidate KHÔNG thể
 * gọi những endpoint đó để biết mình được thi kỳ thi nào — nhưng luồng
 * nghiệp vụ mục 30 prompt gốc lại yêu cầu "Candidate → chọn Exam → bắt đầu
 * phiên thi", tức phải có MỘT cách nào đó cho Candidate xem danh sách kỳ
 * thi khả dụng.
 *
 * Route Frontend `/candidate/exams` gọi một endpoint MỚI, giả định:
 * `GET /exams/available` (Authorize CANDIDATE hoặc bất kỳ role đã đăng
 * nhập), trả về các Exam ở trạng thái SCHEDULED/ONGOING — endpoint này
 * CHƯA XÁC NHẬN có tồn tại trong backend thật hay chưa. Nếu backend chưa
 * có, đây là việc backend cần bổ sung trước khi Part 3 chạy được end-to-end
 * (xem mục "Việc BẮT BUỘC" trong integration guide).
 */
export interface CandidateExamSummary {
  examId: string;
  examCode: string;
  examName: string;
  description: string | null;
  examDate: string;
  startTime: string;
  endTime: string;
  status: "SCHEDULED" | "ONGOING";
}
