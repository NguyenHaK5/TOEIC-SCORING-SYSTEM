"use client";

import type { UseMutationResult } from "@tanstack/react-query";
import { AudioBlock } from "@/components/candidate/audio-block";
import { McQuestionCard } from "@/components/candidate/mc-question-card";
import { WritingQuestionCard } from "@/components/candidate/writing-question-card";
import type { ExamSessionQuestion, SaveAnswerPayload } from "@/types/exam-session";

interface GroupedQuestion {
  question: ExamSessionQuestion;
  showAudio: boolean;
}

/** Hàm thuần, nằm NGOÀI component — gom các câu hỏi dùng chung 1 audioUrl
 * liên tiếp thành 1 player duy nhất. Đặt ngoài component để tránh vi phạm
 * quy tắc "không mutate biến cục bộ trong thân component" của React
 * Compiler (bật sẵn trong eslint-config-next ở Next.js 16). */
function groupByAudio(questions: ExamSessionQuestion[]): GroupedQuestion[] {
  const result: GroupedQuestion[] = [];
  let lastAudioUrl: string | null = null;
  for (const question of questions) {
    const showAudio = Boolean(question.audioUrl) && question.audioUrl !== lastAudioUrl;
    if (question.audioUrl) lastAudioUrl = question.audioUrl;
    result.push({ question, showAudio });
  }
  return result;
}

export function SectionPanel({
  questions,
  saveMutation,
  disabled,
}: {
  questions: ExamSessionQuestion[];
  saveMutation: UseMutationResult<void, unknown, SaveAnswerPayload>;
  disabled: boolean;
}) {
  const sorted = questions.slice().sort((a, b) => a.questionNumber - b.questionNumber);
  const grouped = groupByAudio(sorted);

  return (
    <div className="space-y-5">
      {grouped.map(({ question: q, showAudio }) => (
        <div key={q.questionId} className="space-y-3">
          {showAudio && q.audioUrl && <AudioBlock audioUrl={q.audioUrl} />}
          {q.questionType === "MULTIPLE_CHOICE" ? (
            <McQuestionCard question={q} saveMutation={saveMutation} disabled={disabled} />
          ) : (
            <WritingQuestionCard
              key={q.questionId}
              question={q}
              saveMutation={saveMutation}
              disabled={disabled}
            />
          )}
        </div>
      ))}
    </div>
  );
}
