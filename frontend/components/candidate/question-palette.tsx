import { cn } from "@/lib/utils";
import type { ExamSessionQuestion } from "@/types/exam-session";

function isAnswered(q: ExamSessionQuestion): boolean {
  return Boolean(q.selectedOptionId) || Boolean(q.answerText && q.answerText.trim().length > 0);
}

export function QuestionPalette({
  questions,
  activeQuestionId,
  onJump,
}: {
  questions: ExamSessionQuestion[];
  activeQuestionId: string | null;
  onJump: (questionId: string) => void;
}) {
  const answeredCount = questions.filter(isAnswered).length;

  return (
    <div>
      <p className="text-[13px] text-muted">
        Đã trả lời {answeredCount}/{questions.length}
      </p>
      <div className="mt-3 grid grid-cols-6 gap-1.5">
        {questions
          .slice()
          .sort((a, b) => a.questionNumber - b.questionNumber)
          .map((q) => (
            <button
              key={q.questionId}
              type="button"
              onClick={() => onJump(q.questionId)}
              className={cn(
                "flex h-8 w-8 items-center justify-center rounded-sm border text-[12px] font-medium transition-colors",
                q.questionId === activeQuestionId
                  ? "border-teal bg-teal text-paper-raised"
                  : isAnswered(q)
                    ? "border-teal/40 bg-teal/10 text-teal-dark"
                    : "border-border bg-paper-raised text-muted hover:border-teal/40"
              )}
            >
              {q.questionNumber}
            </button>
          ))}
      </div>
    </div>
  );
}
