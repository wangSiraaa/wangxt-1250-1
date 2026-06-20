<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h3 style="margin:0">整改管理</h3>
        <span style="color:#909399;font-size:13px">整改联动：未关闭整改期间，同塔吊仅允许执行低风险任务</span>
      </div>
      <div class="header-actions">
        <el-select v-model="statusFilter" placeholder="整改状态" clearable style="width:140px">
          <el-option v-for="(item, key) in RectificationStatusMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-select v-model="priorityFilter" placeholder="优先级" clearable style="width:120px">
          <el-option v-for="(item, key) in RectificationPriorityMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-select v-model="craneFilter" placeholder="关联塔吊" clearable style="width:140px">
          <el-option v-for="c in towerCranes" :key="c.craneNo" :label="c.craneNo" />
        </el-select>
      </div>
    </div>

    <el-row :gutter="16" style="margin-bottom:16px">
      <el-col :span="6">
        <el-card shadow="hover">
          <el-statistic title="待执行" :value="pendingCount" :value-style="{ color: '#F56C6C' }" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <el-statistic title="执行中" :value="executingCount" :value-style="{ color: '#409EFF' }" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <el-statistic title="待审核" :value="reviewingCount" :value-style="{ color: '#E6A23C' }" />
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <el-statistic title="已超期" :value="overdueCount" :value-style="{ color: '#909399' }" />
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="hover">
      <el-table :data="filteredRectifications" stripe v-loading="loading">
        <el-table-column label="编号" prop="rectificationNo" width="150" align="center" fixed />
        <el-table-column label="关联塔吊" width="110">
          <template #default="{ row }">{{ row.towerCrane?.craneNo || '—' }}</template>
        </el-table-column>
        <el-table-column label="来源" width="100">
          <template #default="{ row }">{{ row.sourceAlarmId ? '报警触发' : '巡检发现' }}</template>
        </el-table-column>
        <el-table-column label="关联报警" width="100">
          <template #default="{ row }">{{ row.sourceAlarmId ? `AL-${row.sourceAlarmId}` : '—' }}</template>
        </el-table-column>
        <el-table-column label="整改要求" min-width="240" show-overflow-tooltip>
          <template #default="{ row }">
            <div style="line-height:1.5">
              <div style="font-weight:500">{{ row.description }}</div>
              <div style="color:#909399;margin-top:4px">问题类别：{{ row.rectificationCategory || '—' }}</div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="优先级" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="RectificationPriorityMap[row.priority].type" effect="dark" size="small">
              {{ RectificationPriorityMap[row.priority].label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="负责人" width="100" align="center">
          <template #default="{ row }">{{ row.assignedTo?.name || '—' }}</template>
        </el-table-column>
        <el-table-column label="截止时间" width="160">
          <template #default="{ row }">
            <span :class="{ 'overdue': isOverdue(row) }">
              {{ formatDateTime(row.dueDate) }}
            </span>
            <el-tag v-if="isOverdue(row) && row.status !== 4" type="danger" effect="dark" size="small" style="margin-left:4px">
              已超期
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="RectificationStatusMap[row.status].type" effect="dark">
              {{ RectificationStatusMap[row.status].label }}
            </el-tag>
            <el-tag v-if="row.restrictsHighRiskTasks" type="warning" effect="plain" size="small" style="margin-top:4px">
              限制高风险
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="280" fixed="right" align="center">
          <template #default="{ row }">
            <template v-if="row.status === 1">
              <el-button type="primary" link size="small" @click="assignRectification(row)">分配</el-button>
              <el-button type="info" link size="small" @click="editRectification(row)">编辑</el-button>
            </template>
            <template v-if="row.status === 1 || row.status === 2">
              <el-button type="success" link size="small" @click="executeRectification(row)">执行</el-button>
            </template>
            <template v-if="row.status === 3">
              <el-button type="primary" link size="small" @click="reviewRectification(row)">审核</el-button>
            </template>
            <el-button type="info" link size="small" @click="viewDetail(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="rectDialogVisible" :title="rectDialogTitle" width="700px" destroy-on-close>
      <el-form :model="rectForm" label-width="110px" :rules="rectRules" ref="rectFormRef">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="关联塔吊" prop="towerCraneId" required>
              <el-select v-model="rectForm.towerCraneId" placeholder="选择塔吊" style="width:100%">
                <el-option v-for="c in towerCranes" :key="c.id" :label="c.craneNo" :value="c.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="优先级" prop="priority" required>
              <el-select v-model="rectForm.priority" placeholder="选择优先级" style="width:100%">
                <el-option v-for="(item, key) in RectificationPriorityMap" :key="key" :label="item.label" :value="Number(key)" />
              </el-select>
              <span style="color:#909399;font-size:12px;display:block;margin-top:4px">
                截止时间将按优先级自动设定
              </span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="问题描述" prop="description" required>
            <el-input v-model="rectForm.description" type="textarea" :rows="3" placeholder="描述需要整改的问题详情" />
          </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="整改要求" prop="rectificationCategory">
            <el-input v-model="rectForm.rectificationCategory" placeholder="整改类别" />
          </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="整改措施" prop="actionRequired">
          <el-input v-model="rectForm.actionRequired" type="textarea" :rows="2" placeholder="具体需要采取的整改措施" />
        </el-form-item>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="负责人" prop="assignedToId">
              <el-select v-model="rectForm.assignedToId" placeholder="选择负责人" style="width:100%">
                <el-option v-for="p in allPersons" :key="p.id" :label="p.name" :value="p.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="截止时间" prop="dueDate" required>
              <el-date-picker v-model="rectForm.dueDate" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width:100%" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="rectDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveRectification">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="executeDialogVisible" title="执行整改" width="600px">
      <el-form :model="executeForm" label-width="100px">
        <el-descriptions :column="2" border size="small" style="margin-bottom:16px">
          <el-descriptions-item label="整改编号">{{ currentRectification?.rectificationNo }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="RectificationStatusMap[currentRectification!.status].type">
              {{ RectificationStatusMap[currentRectification!.status].label }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="问题" :span="2">{{ currentRectification?.description }}</el-descriptions-item>
        </el-descriptions>
        <el-form-item label="执行措施" required>
          <el-input v-model="executeForm.actionsTaken" type="textarea" :rows="4" placeholder="描述已采取的整改措施" />
        </el-form-item>
        <el-form-item label="执行结果" required>
          <el-input v-model="executeForm.results" type="textarea" :rows="2" placeholder="整改结果描述，如：限位器已重新标定..." />
        </el-form-item>
        <el-form-item label="证据">
          <el-upload action="#" list-type="picture-card" :auto-upload="false">
            <el-icon><Plus /></el-icon>
          </el-upload>
          <div style="color:#909399;font-size:12px;margin-top:4px">整改完成证据上传（可选）</div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="executeDialogVisible = false">取消</el-button>
        <el-button type="success" @click="confirmExecute">提交审核</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="reviewDialogVisible" title="监理审核" width="600px">
      <div v-if="currentRectification" style="margin-bottom:16px">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="整改编号">{{ currentRectification.rectificationNo }}</el-descriptions-item>
          <el-descriptions-item label="塔吊">{{ currentRectification.towerCrane?.craneNo }}</el-descriptions-item>
          <el-descriptions-item label="问题描述" :span="2">{{ currentRectification.description }}</el-descriptions-item>
          <el-descriptions-item label="执行措施" :span="2">{{ currentRectification.actionsTaken || '—' }}</el-descriptions-item>
          <el-descriptions-item label="执行结果" :span="2">{{ currentRectification.results || '—' }}</el-descriptions-item>
        </el-descriptions>
      </div>
      <div class="linkage-warning">
        <div class="warning-title"><el-icon><Tips /></el-icon>审核联动说明</div>
        <div class="warning-content">
          <p><b>审核通过</b>：整改关闭，自动恢复塔吊状态，塔吊可执行正常任务（解除低风险限制）</p>
          <p><b>审核驳回</b>：返回执行状态，需重新执行整改</p>
        </div>
      </div>
      <el-form :model="reviewForm" label-width="100px" style="margin-top:16px">
        <el-form-item label="审核人" required>
          <el-select v-model="reviewForm.reviewerId" placeholder="选择监理" style="width:100%">
            <el-option v-for="s in supervisors" :key="s.id" :label="s.name" :value="s.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="审核意见" required>
          <el-input v-model="reviewForm.comments" type="textarea" :rows="3" placeholder="审核意见说明" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="reviewDialogVisible = false">取消</el-button>
        <el-button type="danger" @click="confirmReview(false)">审核驳回</el-button>
        <el-button type="success" @click="confirmReview(true)">审核通过</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Tips } from '@element-plus/icons-vue'
import dayjs from 'dayjs'
import api from '@/api'
import { useAppStore } from '@/store/app'
import type { Rectification, TowerCrane, Person } from '@/api'
import {
  RectificationStatusMap, RectificationPriorityMap
} from '@/utils/enumMaps'

const appStore = useAppStore()

const loading = ref(false)
const saving = ref(false)

const statusFilter = ref<number | null>(null)
const priorityFilter = ref<number | null>(null)
const craneFilter = ref<string | null>(null)

const rectDialogVisible = ref(false)
const executeDialogVisible = ref(false)
const reviewDialogVisible = ref(false)

const isEdit = ref(false)
const currentRectification = ref<Rectification | null>(null)

const rectDialogTitle = computed(() => isEdit.value ? '编辑整改' : '新建整改')

const towerCranes = computed<TowerCrane[]>(() => appStore.towerCranes)
const allPersons = computed<Person[]>(() => appStore.persons.filter(p => p.isActive))
const supervisors = computed<Person[]>(() => appStore.persons.filter(p => p.role === 3 && p.isActive))

const rectForm = reactive<Partial<Rectification>>({
  towerCraneId: undefined, sourceAlarmId: undefined,
  description: '', rectificationCategory: '', actionRequired: '',
  priority: 2, assignedToId: undefined, dueDate: ''
})
const executeForm = reactive({ actionsTaken: '', results: '' })
const reviewForm = reactive({ reviewerId: undefined as number | undefined, comments: '' })

const rectFormRef = ref<FormInstance>()

const rectRules: FormRules = {
  towerCraneId: [{ required: true, message: '请选择塔吊', trigger: 'change' }],
  description: [{ required: true, message: '请输入问题描述', trigger: 'blur' }],
  priority: [{ required: true, message: '请选择优先级', trigger: 'change' }],
  dueDate: [{ required: true, message: '请选择截止时间', trigger: 'change' }]
}

const filteredRectifications = computed(() => {
  let list = appStore.rectifications
  if (statusFilter.value !== null) list = list.filter(r => r.status === statusFilter.value)
  if (priorityFilter.value !== null) list = list.filter(r => r.priority === priorityFilter.value)
  if (craneFilter.value) list = list.filter(r => r.towerCrane?.craneNo === craneFilter.value)
  return list.sort((a, b) => dayjs(b.createdAt).valueOf() - dayjs(a.createdAt).valueOf())
})

const pendingCount = computed(() => appStore.rectifications.filter(r => r.status === 1).length)
const executingCount = computed(() => appStore.rectifications.filter(r => r.status === 2).length)
const reviewingCount = computed(() => appStore.rectifications.filter(r => r.status === 3).length)
const overdueCount = computed(() => {
  const now = dayjs()
  return appStore.rectifications.filter(r =>
    r.status !== 4 && r.dueDate && dayjs(r.dueDate).isBefore(now)
    ).length
})

const formatDateTime = (d: string) => dayjs(d).format('YYYY-MM-DD HH:mm')

const isOverdue = (r: Rectification) => r.dueDate && dayjs(r.dueDate).isBefore(dayjs())

const autoSetDueDate = () => {
  if (!rectForm.priority) return
  const days = rectForm.priority === 1 ? 1 : rectForm.priority === 2 ? 2 : 3
  rectForm.dueDate = dayjs().add(days, 'day').format('YYYY-MM-DDTHH:mm:ss')
}

watch(() => rectForm.priority, autoSetDueDate)

const editRectification = (rect: Rectification) => {
  isEdit.value = true
  currentRectification.value = rect
  Object.assign(rectForm, {
    towerCraneId: rect.towerCraneId,
    description: rect.description,
    rectificationCategory: rect.rectificationCategory || '',
    actionRequired: rect.actionRequired || '',
    priority: rect.priority,
    assignedToId: rect.assignedToId,
    dueDate: rect.dueDate
  })
  rectDialogVisible.value = true
}

const assignRectification = (rect: Rectification) => {
  ElMessageBox.prompt('请选择整改负责人', '分配整改', {
    inputType: 'select',
    inputOptions: allPersons.value.map(p => ({ value: String(p.id), label: p.name }))
  }).then(async ({ value }) => {
    try {
      await api.rectifications.assign(rect.id, Number(value))
      ElMessage.success('分配成功')
      await appStore.fetchRectifications()
    } catch (e) { /* handled */ }
  }).catch(() => {})
}

const executeRectification = (rect: Rectification) => {
  currentRectification.value = rect
  Object.assign(executeForm, { actionsTaken: rect.actionsTaken || '', results: rect.results || '' })
  executeDialogVisible.value = true
}

const confirmExecute = async () => {
  if (!executeForm.actionsTaken.trim() || !executeForm.results.trim()) {
    ElMessage.warning('请填写执行措施和结果')
    return
  }
  try {
    await api.rectifications.execute(currentRectification!.id, executeForm.actionsTaken, executeForm.results)
    ElMessage.success('整改执行完成，已提交审核')
    executeDialogVisible.value = false
    await appStore.fetchRectifications()
  } catch (e) { /* handled */ }
}

const reviewRectification = async (rect: Rectification) => {
  if (!supervisors.value.length) await appStore.fetchPersons()
  currentRectification.value = rect
  Object.assign(reviewForm, { reviewerId: undefined, comments: '' })
  reviewDialogVisible.value = true
}

const confirmReview = async (approved: boolean) => {
  if (!reviewForm.reviewerId || !reviewForm.comments.trim()) {
    ElMessage.warning('请填写审核信息')
    return
  }
  const msg = approved ? '确定审核通过？将关闭整改，恢复塔吊状态' : '确定审核驳回？整改将返回执行状态'
  ElMessageBox.confirm(msg, '审核确认', { type: approved ? 'success' : 'warning' })
    .then(async () => {
      try {
        await api.rectifications.review(currentRectification!.id, reviewForm.reviewerId!, approved, reviewForm.comments)
        ElMessage.success(approved ? '审核通过，整改已关闭，塔吊状态恢复' : '已驳回，返回执行')
        reviewDialogVisible.value = false
        await Promise.all([
          appStore.fetchRectifications(),
          appStore.fetchTowerCranes(),
          appStore.fetchLiftingTasks()
        ])
      } catch (e) { /* handled */ }
    }).catch(() => {})
}

const saveRectification = async () => {
  await rectFormRef.value?.validate()
  saving.value = true
  try {
    if (isEdit.value && rectForm.id) {
      await api.rectifications.update(rectForm.id, rectForm)
      ElMessage.success('编辑成功')
    } else {
      await api.rectifications.create(rectForm)
      ElMessage.success('整改创建成功')
    }
    rectDialogVisible.value = false
    await Promise.all([appStore.fetchRectifications(), appStore.fetchTowerCranes()])
  } finally {
    saving.value = false
  }
}

const viewDetail = (rect: Rectification) => {
  ElMessageBox.alert(
    `
      <div style="line-height:1.8">
        <p><b>整改编号：</b>${rect.rectificationNo}</p>
        <p><b>状态：</b>${RectificationStatusMap[rect.status].label}（${RectificationPriorityMap[rect.priority].label}）</p>
        <p><b>塔吊：</b>${rect.towerCrane?.craneNo || '—'}</p>
        <p><b>来源：</b>${rect.sourceAlarmId ? '报警 AL-' + rect.sourceAlarmId : '巡检发现'}</p>
        <p><b>负责人：</b>${rect.assignedTo?.name || '—'}</p>
        <p><b>问题描述：</b>${rect.description}</p>
        ${rect.actionRequired ? `<p><b>整改措施：</b>${rect.actionRequired}</p>` : ''}
        ${rect.actionsTaken ? `<p><b>已执行：</b>${rect.actionsTaken}</p>` : ''}
        ${rect.results ? `<p><b>执行结果：</b>${rect.results}</p>` : ''}
        ${rect.reviewComments ? `<p><b>审核意见：</b>${rect.reviewComments}</p>` : ''}
        <p><b>截止时间：</b>${formatDateTime(rect.dueDate)}</p>
        <p><b>创建时间：</b>${formatDateTime(rect.createdAt)}</p>
        ${rect.restrictsHighRiskTasks ? '<p style="color:#E6A23C"><b>联动：</b>当前限制高风险任务</p>' : ''}
      </div>
    `,
    '整改详情',
    { dangerouslyUseHTMLString: true, confirmButtonText: '关闭', width: '500px' }
  )
}

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([
      appStore.fetchRectifications(), appStore.fetchTowerCranes(), appStore.fetchPersons()])
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.overdue {
  color: #F56C6C;
  font-weight: 500;
}
</style>
