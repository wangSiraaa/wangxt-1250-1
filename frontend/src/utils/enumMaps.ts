export const UserRoleMap: Record<number, { label: string; type: string }> = {
  1: { label: '安全员', type: 'primary' },
  2: { label: '司机', type: 'success' },
  3: { label: '监理', type: 'warning' },
  4: { label: '管理员', type: 'danger' }
}

export const DriverStatusMap: Record<number, { label: string; type: string }> = {
  1: { label: '空闲', type: 'info' },
  2: { label: '作业中', type: 'success' },
  3: { label: '停职', type: 'danger' }
}

export const TowerCraneStatusMap: Record<number, { label: string; type: string; dot?: boolean }> = {
  1: { label: '空闲', type: 'success' },
  2: { label: '作业中', type: 'primary' },
  3: { label: '预警', type: 'warning' },
  4: { label: '维护中', type: 'info' },
  5: { label: '停用', type: 'danger' }
}

export const TaskRiskLevelMap: Record<number, { label: string; type: string }> = {
  1: { label: '低风险', type: 'success' },
  2: { label: '中风险', type: 'warning' },
  3: { label: '高风险', type: 'danger' }
}

export const TaskStatusMap: Record<number, { label: string; type: string }> = {
  1: { label: '草稿', type: 'info' },
  2: { label: '待司机确认', type: 'warning' },
  3: { label: '进行中', type: 'primary' },
  4: { label: '已完成', type: 'success' },
  5: { label: '已取消', type: 'info' },
  6: { label: '已暂停', type: 'danger' }
}

export const AlarmTypeMap: Record<number, { label: string; type: string }> = {
  1: { label: '超载', type: 'danger' },
  2: { label: '高度限位', type: 'warning' },
  3: { label: '幅度限位', type: 'warning' },
  4: { label: '回转限位', type: 'warning' },
  5: { label: '倾翻预警', type: 'danger' },
  6: { label: '风速预警', type: 'warning' },
  7: { label: '碰撞预警', type: 'danger' }
}

export const AlarmLevelMap: Record<number, { label: string; type: string }> = {
  1: { label: '提示', type: 'info' },
  2: { label: '警告', type: 'warning' },
  3: { label: '严重', type: 'danger' }
}

export const AlarmStatusMap: Record<number, { label: string; type: string }> = {
  1: { label: '待处理', type: 'warning' },
  2: { label: '处理中', type: 'primary' },
  3: { label: '已解决', type: 'success' },
  4: { label: '已忽略', type: 'info' }
}

export const RectificationStatusMap: Record<number, { label: string; type: string }> = {
  1: { label: '待整改', type: 'warning' },
  2: { label: '整改中', type: 'primary' },
  3: { label: '待审核', type: 'warning' },
  4: { label: '已关闭', type: 'success' },
  5: { label: '已驳回', type: 'danger' }
}

export const RectificationPriorityMap: Record<number, { label: string; type: string }> = {
  1: { label: '低', type: 'info' },
  2: { label: '中', type: 'warning' },
  3: { label: '高', type: 'danger' },
  4: { label: '紧急', type: 'danger' }
}

export const CertificateTypeMap: Record<number, { label: string }> = {
  1: { label: '塔吊操作证' },
  2: { label: '安全员证' },
  3: { label: '监理员证' },
  4: { label: '特种设备作业证' }
}
