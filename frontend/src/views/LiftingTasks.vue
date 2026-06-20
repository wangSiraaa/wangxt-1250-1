<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h3 style="margin:0">吊装任务管理</h3>
        <span style="color:#909399;font-size:13px">安全员登记任务 → 司机确认上机 → 任务执行 → 完成，全程联动资质和报警</span>
      </div>
      <div class="header-actions">
        <el-select v-model="statusFilter" placeholder="任务状态" clearable style="width:150px">
          <el-option v-for="(item, key) in TaskStatusMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-select v-model="riskFilter" placeholder="风险等级" clearable style="width:120px">
          <el-option v-for="(item, key) in TaskRiskLevelMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-select v-model="craneFilter" placeholder="塔吊" clearable style="width:140px">
          <el-option v-for="c in towerCranes" :key="c.id" :label="c.craneNo" :value="c.id" />
        </el-select>
        <el-button type="primary" :icon="Plus" @click="openAddDialog">登记吊装任务</el-button>
      </div>
    </div>

    <el-card shadow="hover">
      <el-table :data="filteredTasks" stripe v-loading="loading">
        <el-table-column label="任务编号" prop="taskNo" width="160" align="center" fixed />
        <el-table-column label="塔吊" width="100" align="center">
          <template #default="{ row }">{{ row.towerCrane?.craneNo || '—' }}</template>
        </el-table-column>
        <el-table-column label="作业描述" prop="description" min-width="200" show-overflow-tooltip />
        <el-table-column label="位置" prop="location" width="140" />
        <el-table-column label="载荷/半径/高度" width="150" align="center">
          <template #default="{ row }">
            {{ row.plannedLoad }}吨 / {{ row.radius }}m / {{ row.liftHeight }}m
          </template>
        </el-table-column>
        <el-table-column label="风险" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="TaskRiskLevelMap[row.riskLevel].type" effect="dark" size="small">
              {{ TaskRiskLevelMap[row.riskLevel].label }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="安全员/司机" width="160">
          <template #default="{ row }">
            <div style="font-size:12px;line-height:1.6">
              <div>{{ row.safetyOfficer?.name || '—' }}</div>
              <div style="color:#909399">{{ row.driver?.name || '待确认' }}</div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="TaskStatusMap[row.status].type" effect="dark" size="small">
              {{ TaskStatusMap[row.status].label }}
            </el-tag>
            <el-tag v-if="row.isLowRiskOnly" type="warning" effect="plain" size="small" style="margin-top:4px">
              仅低风险
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="计划时间" width="170">
          <template #default="{ row }">
            <div style="font-size:12px;line-height:1.6">
              <div>起: {{ formatDateTime(row.plannedStartTime) }}</div>
              <div style="color:#909399">止: {{ formatDateTime(row.plannedEndTime) }}</div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="340" fixed="right" align="center">
          <template #default="{ row }">
            <template v-if="row.status === 1">
              <el-button type="primary" link size="small" @click="submitTask(row)">提交</el-button>
              <el-button type="success" link size="small" @click="openEditDialog(row)">编辑</el-button>
            </template>
            <template v-if="row.status === 2">
              <el-button type="success" link size="small" @click="driverConfirm(row)">司机确认</el-button>
              <el-button type="primary" link size="small" @click="validateAndStart(row)" :disabled="!row.driverId">启动</el-button>
            </template>
            <template v-if="row.status === 3">
              <el-button type="success" link size="small" @click="completeTask(row)">完成</el-button>
            </template>
            <template v-if="row.status === 6">
              <el-button type="primary" link size="small" @click="resumeTask(row)">恢复</el-button>
            </template>
            <el-button
              v-if="row.status !== 4 && row.status !== 5"
              type="danger" link size="small"
              @click="cancelTask(row)"
            >取消</el-button>
            <el-button type="info" link size="small" @click="viewDetail(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="taskDialogVisible" :title="isEdit ? '编辑吊装任务' : '登记吊装任务'" width="800px" destroy-on-close>
      <el-form :model="taskForm" label-width="100px" :rules="taskRules" ref="taskFormRef">
        <div class="linkage-warning" v-if="validationMessages.length">
          <div class="warning-title"><el-icon><Warning /></el-icon>联动校验提示</div>
          <div class="warning-content">
            <ul style="padding-left:20px;margin:0">
              <li v-for="(msg, i) in validationMessages" :key="i">{{ msg }}</li>
            </ul>
          </div>
        </div>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="塔吊" prop="towerCraneId" required>
              <el-select v-model="taskForm.towerCraneId" placeholder="选择塔吊" style="width:100%" @change="onCraneChange">
                <el-option v-for="c in towerCranes" :key="c.id" :label="c.craneNo + ' - ' + c.model" :value="c.id">
                  <span style="float:left">{{ c.craneNo }} ({{ c.model }})</span>
                  <span style="float:right;color:#909399;font-size:12px">
                    额载{{ c.ratedLoadCapacity }}吨
                  </span>
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="安全员" prop="safetyOfficerId" required>
              <el-select v-model="taskForm.safetyOfficerId" placeholder="选择安全员" style="width:100%">
                <el-option v-for="s in safetyOfficers" :key="s.id" :label="s.name + ' - ' + s.employeeNo" :value="s.id" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="作业位置" prop="location" required>
              <el-input v-model="taskForm.location" placeholder="如 A区1号楼三层" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="风险等级" prop="riskLevel" required>
              <el-select v-model="taskForm.riskLevel" placeholder="选择风险等级" style="width:100%">
                <el-option v-for="(item, key) in TaskRiskLevelMap" :key="key" :label="item.label" :value="Number(key)" />
              </el-select>
              <span v-if="selectedCrane?.rectifications?.some(r => r.status !== 4)" style="color:#E6A23C;font-size:12px;display:block;margin-top:4px">
                该塔吊存在未关闭整改，仅允许低风险
              </span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="作业描述" prop="description" required>
          <el-input v-model="taskForm.description" type="textarea" :rows="2" placeholder="描述吊装作业内容" />
        </el-form-item>

        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="载荷(吨)" prop="plannedLoad" required>
              <el-input-number v-model="taskForm.plannedLoad" :min="0" :precision="2" :step="0.1" style="width:100%" />
              <span v-if="selectedCrane && taskForm.plannedLoad > selectedCrane.ratedLoadCapacity" style="color:#F56C6C;font-size:12px;display:block;margin-top:4px">
                超过额定载荷 {{ selectedCrane.ratedLoadCapacity }} 吨
              </span>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="工作半径(m)" prop="radius" required>
              <el-input-number v-model="taskForm.radius" :min="0" :precision="2" style="width:100%" />
              <span v-if="selectedCrane && taskForm.radius > selectedCrane.maxRadius" style="color:#F56C6C;font-size:12px;display:block;margin-top:4px">
                超过最大半径 {{ selectedCrane.maxRadius }} m
              </span>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="起升高度(m)" prop="liftHeight" required>
              <el-input-number v-model="taskForm.liftHeight" :min="0" :precision="2" style="width:100%" />
              <span v-if="selectedCrane && taskForm.liftHeight > selectedCrane.maxHeight" style="color:#F56C6C;font-size:12px;display:block;margin-top:4px">
                超过最大高度 {{ selectedCrane.maxHeight }} m
              </span>
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="载荷类型">
              <el-input v-model="taskForm.loadType" placeholder="如 钢筋、钢模板、预制构件" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="计划开始" required>
              <el-date-picker v-model="taskForm.plannedStartTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="计划结束" required>
              <el-date-picker v-model="taskForm.plannedEndTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width:100%" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="taskDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveTask">保存</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="detailDrawerVisible" title="任务详情" size="600px">
      <div v-if="currentTask" style="padding:0 20px">
        <el-steps :active="getTaskStep(currentTask)" finish-status="success" align-center style="margin-bottom:24px">
          <el-step title="草稿登记" description="安全员创建" />
          <el-step title="司机确认" description="资质校验" />
          <el-step title="执行吊装" description="实时监控" />
          <el-step title="任务完成" description="记录归档" />
        </el-steps>

        <el-descriptions title="基本信息" :column="2" border size="small">
          <el-descriptions-item label="任务编号">{{ currentTask.taskNo }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="TaskStatusMap[currentTask.status].type">{{ TaskStatusMap[currentTask.status].label }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="塔吊">{{ currentTask.towerCrane?.craneNo }} ({{ currentTask.towerCrane?.model }})</el-descriptions-item>
          <el-descriptions-item label="位置">{{ currentTask.location }}</el-descriptions-item>
          <el-descriptions-item label="安全员">{{ currentTask.safetyOfficer?.name || '—' }}</el-descriptions-item>
          <el-descriptions-item label="司机">
            {{ currentTask.driver?.name || '—' }}
            <span v-if="currentTask.driverConfirmTime" style="color:#909399;font-size:12px">
              ({{ formatDateTime(currentTask.driverConfirmTime) }}确认)
            </span>
          </el-descriptions-item>
          <el-descriptions-item label="风险等级">
            <el-tag :type="TaskRiskLevelMap[currentTask.riskLevel].type">
              {{ TaskRiskLevelMap[currentTask.riskLevel].label }}
            </el-tag>
            <el-tag v-if="currentTask.isLowRiskOnly" type="warning" effect="plain" style="margin-left:4px">
              整改限制：仅低风险
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="作业内容" :span="2">{{ currentTask.description }}</el-descriptions-item>
        </el-descriptions>

        <el-divider>联动数据</el-divider>
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="计划载荷">{{ currentTask.plannedLoad }} 吨</el-descriptions-item>
          <el-descriptions-item label="工作半径">{{ currentTask.radius }} m</el-descriptions-item>
          <el-descriptions-item label="起升高度">{{ currentTask.liftHeight }} m</el-descriptions-item>
          <el-descriptions-item label="载荷类型">{{ currentTask.loadType || '—' }}</el-descriptions-item>
          <el-descriptions-item label="计划开始">{{ formatDateTime(currentTask.plannedStartTime) }}</el-descriptions-item>
          <el-descriptions-item label="计划结束">{{ formatDateTime(currentTask.plannedEndTime) }}</el-descriptions-item>
          <el-descriptions-item label="实际开始">{{ currentTask.actualStartTime ? formatDateTime(currentTask.actualStartTime) : '—' }}</el-descriptions-item>
          <el-descriptions-item label="实际结束">{{ currentTask.actualEndTime ? formatDateTime(currentTask.actualEndTime) : '—' }}</el-descriptions-item>
        </el-descriptions>

        <el-divider>关联报警（{{ currentTask.alarms?.length || 0 }}）</el-divider>
        <el-table :data="currentTask.alarms || []" size="small" stripe>
          <el-table-column label="类型" width="100">
            <template #default="{ row }">
              <el-tag :type="AlarmTypeMap[row.alarmType].type" size="small">{{ AlarmTypeMap[row.alarmType].label }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="级别" width="80" align="center">
            <template #default="{ row }">
              <el-tag :type="AlarmLevelMap[row.alarmLevel].type" size="small" effect="dark">{{ AlarmLevelMap[row.alarmLevel].label }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="描述" prop="description" show-overflow-tooltip />
          <el-table-column label="时间" width="160">
            <template #default="{ row }">{{ formatDateTime(row.alarmTime) }}</template>
          </el-table-column>
          <el-table-column label="状态" width="90" align="center">
            <template #default="{ row }">
              <el-tag :type="AlarmStatusMap[row.status].type" size="small">{{ AlarmStatusMap[row.status].label }}</el-tag>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, watch, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Warning } from '@element-plus/icons-vue'
import dayjs from 'dayjs'
import api from '@/api'
import { useAppStore } from '@/store/app'
import type { LiftingTask, TowerCrane, Person } from '@/api'
import {
  TaskStatusMap, TaskRiskLevelMap, AlarmTypeMap, AlarmLevelMap, AlarmStatusMap
} from '@/utils/enumMaps'

const appStore = useAppStore()
const loading = ref(false)
const saving = ref(false)

const statusFilter = ref<number | null>(null)
const riskFilter = ref<number | null>(null)
const craneFilter = ref<number | null>(null)

const taskDialogVisible = ref(false)
const detailDrawerVisible = ref(false)
const isEdit = ref(false)
const currentTask = ref<LiftingTask | null>(null)

const towerCranes = computed<TowerCrane[]>(() => appStore.towerCranes.filter(c => c.isActive))
const safetyOfficers = computed<Person[]>(() => appStore.persons.filter(p => p.role === 1 && p.isActive))
const drivers = computed<Person[]>(() => appStore.persons.filter(p => p.role === 2 && p.isActive))

const taskForm = reactive<Partial<LiftingTask>>({
  towerCraneId: undefined, safetyOfficerId: undefined, driverId: undefined,
  description: '', location: '', plannedLoad: 0, loadType: '', radius: 0, liftHeight: 0,
  riskLevel: 1, plannedStartTime: '', plannedEndTime: ''
})

const validationMessages = ref<string[]>([])

const taskFormRef = ref<FormInstance>()

const taskRules: FormRules = {
  towerCraneId: [{ required: true, message: '请选择塔吊', trigger: 'change' }],
  safetyOfficerId: [{ required: true, message: '请选择安全员', trigger: 'change' }],
  description: [{ required: true, message: '请输入作业描述', trigger: 'blur' }],
  location: [{ required: true, message: '请输入作业位置', trigger: 'blur' }],
  plannedLoad: [{ required: true, message: '请输入计划载荷', trigger: 'blur' }],
  radius: [{ required: true, message: '请输入工作半径', trigger: 'blur' }],
  liftHeight: [{ required: true, message: '请输入起升高度', trigger: 'blur' }],
  riskLevel: [{ required: true, message: '请选择风险等级', trigger: 'change' }]
}

const selectedCrane = computed<TowerCrane | undefined>(() => {
  return taskForm.towerCraneId ? towerCranes.value.find(c => c.id === taskForm.towerCraneId) : undefined
})

const filteredTasks = computed(() => {
  let list = appStore.liftingTasks
  if (statusFilter.value !== null) list = list.filter(t => t.status === statusFilter.value)
  if (riskFilter.value !== null) list = list.filter(t => t.riskLevel === riskFilter.value)
  if (craneFilter.value !== null) list = list.filter(t => t.towerCraneId === craneFilter.value)
  return list
})

const formatDateTime = (d: string) => dayjs(d).format('YYYY-MM-DD HH:mm')

const getTaskStep = (task: LiftingTask) => {
  switch (task.status) {
    case 1: return 0
    case 2: return 1
    case 3: return 2
    case 4: return 3
    default: return 0
  }
}

const onCraneChange = async () => {
  if (!taskForm.towerCraneId) {
    validationMessages.value = []
    return
  }
  const issues = await api.liftingTasks.validateCreation(taskForm)
  validationMessages.value = issues.data
  const hasOpenRect = await api.towerCranes.hasOpenRectification(taskForm.towerCraneId)
  if (hasOpenRect.data) {
    taskForm.riskLevel = 1
  }
}

const validateTaskForm = async () => {
  const issues = await api.liftingTasks.validateCreation(taskForm)
  validationMessages.value = issues.data
  return issues.data.length === 0
}

const openAddDialog = () => {
  isEdit.value = false
  Object.assign(taskForm, {
    id: undefined, towerCraneId: undefined, safetyOfficerId: undefined, driverId: undefined,
    description: '', location: '', plannedLoad: 0, loadType: '', radius: 0, liftHeight: 0,
    riskLevel: 2,
    plannedStartTime: dayjs().add(1, 'hour').format('YYYY-MM-DDTHH:mm:ss'),
    plannedEndTime: dayjs().add(6, 'hour').format('YYYY-MM-DDTHH:mm:ss')
  })
  validationMessages.value = []
  taskDialogVisible.value = true
}

const openEditDialog = (task: LiftingTask) => {
  isEdit.value = true
  Object.assign(taskForm, { ...task })
  validationMessages.value = []
  taskDialogVisible.value = true
}

const saveTask = async () => {
  await taskFormRef.value?.validate()
  const valid = await validateTaskForm()
  if (!valid) {
    ElMessage.warning('存在联动限制，请先解决提示中的问题')
    return
  }
  saving.value = true
  try {
    if (isEdit.value && taskForm.id) {
      await api.liftingTasks.update(taskForm.id, taskForm)
      ElMessage.success('编辑成功')
    } else {
      await api.liftingTasks.create(taskForm)
      ElMessage.success('任务登记成功')
    }
    taskDialogVisible.value = false
    await appStore.fetchLiftingTasks()
  } finally {
    saving.value = false
  }
}

const submitTask = async (task: LiftingTask) => {
  try {
    await api.liftingTasks.submit(task.id)
    ElMessage.success('已提交，等待司机确认上机')
    await appStore.fetchLiftingTasks()
  } catch (e) { /* handled */ }
}

const driverConfirm = (task: LiftingTask) => {
  ElMessageBox.prompt(
    '请选择司机进行确认上机操作，系统将自动校验司机资质。',
    '司机确认上机',
    {
      confirmButtonText: '确认上机',
      cancelButtonText: '取消',
      inputPattern: /.+/,
      inputErrorMessage: '请选择司机',
      inputType: 'select',
      inputOptions: drivers.value.map(d => {
        const hasExpired = d.certificates?.some(c => c.certificateType === 1 && c.isExpired)
        const hasWarning = d.certificates?.some(c =>
          c.certificateType === 1 && !c.isExpired && (c.daysUntilExpiry || 999) <= 30
        )
        let label = d.name
        if (hasExpired) label += '  [证件已过期 - 禁止上机]'
        else if (hasWarning) label += '  [证件即将到期]'
        if (d.driverStatus === 2) label += '  [作业中]'
        return { value: String(d.id), label, disabled: hasExpired || d.driverStatus === 2 }
      }),
      inputValue: task.driverId ? String(task.driverId) : ''
    }
  ).then(async ({ value }) => {
    const driverId = Number(value)
    try {
      const { data } = await api.liftingTasks.driverConfirm(task.id, driverId)
      ElMessage.success('司机确认成功，资质校验通过')
      await appStore.fetchLiftingTasks()
    } catch (e) { /* handled */ }
  }).catch(() => {})
}

const validateAndStart = async (task: LiftingTask) => {
  const issues = await api.liftingTasks.validateStart(task.id)
  if (issues.data.length > 0) {
    ElMessageBox.alert(
      `<ul style="padding-left:20px;margin:0">${issues.data.map(i => `<li>${i}</li>`).join('')}</ul>`,
      '启动任务校验失败',
      { dangerouslyUseHTMLString: true, type: 'error' }
    )
    return
  }
  ElMessageBox.confirm(
    `确定启动任务【${task.taskNo}】？启动后将进入实时监控状态。`,
    '启动确认',
    { type: 'info' }
  ).then(async () => {
    try {
      await api.liftingTasks.start(task.id)
      ElMessage.success('任务已启动')
      await Promise.all([appStore.fetchLiftingTasks(), appStore.fetchTowerCranes()])
    } catch (e) { /* handled */ }
  }).catch(() => {})
}

const completeTask = async (task: LiftingTask) => {
  try {
    await api.liftingTasks.complete(task.id)
    ElMessage.success('任务完成')
    await Promise.all([appStore.fetchLiftingTasks(), appStore.fetchTowerCranes()])
  } catch (e) { /* handled */ }
}

const resumeTask = async (task: LiftingTask) => {
  ElMessage.info('请先处理阻断性报警，报警处理后任务将自动恢复')
}

const cancelTask = (task: LiftingTask) => {
  ElMessageBox.prompt('请输入取消原因', '取消任务', {
    confirmButtonText: '确认取消',
    cancelButtonText: '返回',
    inputType: 'textarea',
    inputValidator: v => !!v || '请输入取消原因'
  }).then(async ({ value }) => {
    try {
      await api.liftingTasks.cancel(task.id, value)
      ElMessage.success('任务已取消')
      await Promise.all([appStore.fetchLiftingTasks(), appStore.fetchTowerCranes()])
    } catch (e) { /* handled */ }
  }).catch(() => {})
}

const viewDetail = async (task: LiftingTask) => {
  const { data } = await api.liftingTasks.getById(task.id)
  currentTask.value = data
  detailDrawerVisible.value = true
}

watch(
  [() => taskForm.riskLevel, () => taskForm.plannedLoad, () => taskForm.radius, () => taskForm.liftHeight],
  () => { if (taskForm.towerCraneId) validateTaskForm() }
)

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([
      appStore.fetchLiftingTasks(),
      appStore.fetchTowerCranes(),
      appStore.fetchPersons()
    ])
  } finally {
    loading.value = false
  }
})
</script>
